using System;
using System.Collections.Generic;
using System.Linq;
using AutoDI.Build.CodeGen;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoDI.Build
{
    partial class ProcessAssemblyTask
    {
        //TODO: out parameters... yuck
        private TypeDefinition GenerateAutoDIClass(Mapping mapping, Settings settings, ICodeGenerator codeGenerator,
            out MethodDefinition initMethod)
        {
            var containerType = new TypeDefinition(Constants.Namespace, Constants.TypeName,
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed
                | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit)
            {
                BaseType = Import.System.Object
            };

            FieldDefinition globalServiceProvider =
                ModuleDefinition.CreateStaticReadonlyField(Constants.GlobalServiceProviderName, false, Import.System.IServiceProvider);
            containerType.Fields.Add(globalServiceProvider);

            MethodDefinition configureMethod = GenerateAddServicesMethod(mapping, settings, containerType, codeGenerator);
            containerType.Methods.Add(configureMethod);

            initMethod = GenerateInitMethod(configureMethod, globalServiceProvider);
            containerType.Methods.Add(initMethod);

            MethodDefinition disposeMethod = GenerateDisposeMethod(globalServiceProvider);
            containerType.Methods.Add(disposeMethod);

            return containerType;
        }

        private MethodDefinition GenerateAddServicesMethod(Mapping mapping, Settings settings, TypeDefinition containerType, ICodeGenerator codeGenerator)
        {
            var method = new MethodDefinition("AddServices",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                Import.System.Void);

            var serviceCollection = new ParameterDefinition("collection", ParameterAttributes.None, Import.DependencyInjection.IServiceCollection);
            method.Parameters.Add(serviceCollection);

            IMethodGenerator methodGenerator = codeGenerator?.Method(method);
            ILProcessor processor = method.Body.GetILProcessor();

            VariableDefinition exceptionList = null;
            VariableDefinition exception = null;
            if (settings.DebugExceptions)
            {
                var genericType = ModuleDefinition.ImportReference(Import.System.Collections.List.Type.MakeGenericInstanceType(Import.System.Exception));
                exceptionList = new VariableDefinition(genericType);
                exception = new VariableDefinition(Import.System.Exception);

                method.Body.Variables.Add(exceptionList);
                method.Body.Variables.Add(exception);

                MethodReference listCtor = Import.System.Collections.List.Ctor;
                listCtor = listCtor.MakeGenericDeclaringType(Import.System.Exception);

                Instruction createListInstruction = Instruction.Create(OpCodes.Newobj, listCtor);
                processor.Append(createListInstruction);
                processor.Emit(OpCodes.Stloc, exceptionList);

                methodGenerator?.Append("List<Exception> list = new List<Exception>();", createListInstruction);
                methodGenerator?.Append(Environment.NewLine);
            }

            MethodReference funcCtor = Import.System.Func2Ctor;

            if (mapping != null)
            {
                int factoryIndex = 0;
                var factoryMethods = new Dictionary<string, MethodDefinition>();

                foreach (Registration registration in mapping)
                {
                    try
                    {
                        Logger.Debug($"Processing map for {registration.TargetType.FullName}", DebugLogLevel.Verbose);

                        if (!factoryMethods.TryGetValue(registration.TargetType.FullName,
                            out MethodDefinition factoryMethod))
                        {
                            factoryMethod = GenerateFactoryMethod(registration.TargetType, factoryIndex, codeGenerator);
                            if (factoryMethod is null)
                            {
                                Logger.Debug($"No acceptable constructor for '{registration.TargetType.FullName}', skipping map",
                                    DebugLogLevel.Verbose);
                                continue;
                            }
                            factoryMethods[registration.TargetType.FullName] = factoryMethod;
                            factoryIndex++;
                            containerType.Methods.Add(factoryMethod);
                        }


                        var tryStart = Instruction.Create(OpCodes.Ldarg_0); //collection parameter
                        processor.Append(tryStart);

                        TypeReference importedKey = ModuleDefinition.ImportReference(registration.Key);
                        Logger.Debug(
                            $"Mapping {importedKey.FullName} => {registration.TargetType.FullName} ({registration.Lifetime})",
                            DebugLogLevel.Default);
                        processor.Emit(OpCodes.Ldtoken, importedKey);
                        processor.Emit(OpCodes.Call, Import.System.Type.GetTypeFromHandle);

                        processor.Emit(OpCodes.Ldtoken, ModuleDefinition.ImportReference(registration.TargetType));
                        processor.Emit(OpCodes.Call, Import.System.Type.GetTypeFromHandle);

                        processor.Emit(OpCodes.Ldnull);
                        processor.Emit(OpCodes.Ldftn, factoryMethod);
                        processor.Emit(OpCodes.Newobj,
                            ModuleDefinition.ImportReference(
                                funcCtor.MakeGenericDeclaringType(Import.System.IServiceProvider,
                                    ModuleDefinition.ImportReference(registration.TargetType))));

                        processor.Emit(OpCodes.Ldc_I4, (int)registration.Lifetime);

                        processor.Emit(OpCodes.Call, Import.AutoDI.ServiceCollectionMixins.AddAutoDIService);
                        processor.Emit(OpCodes.Pop);


                        if (settings.DebugExceptions)
                        {
                            Instruction afterCatch = Instruction.Create(OpCodes.Nop);
                            processor.Emit(OpCodes.Leave_S, afterCatch);

                            Instruction handlerStart = Instruction.Create(OpCodes.Stloc, exception);
                            processor.Append(handlerStart);
                            processor.Emit(OpCodes.Ldloc, exceptionList);
                            processor.Emit(OpCodes.Ldstr, $"Error adding type '{registration.TargetType.FullName}' with key '{registration.Key.FullName}'");
                            processor.Emit(OpCodes.Ldloc, exception);

                            processor.Emit(OpCodes.Newobj, Import.AutoDI.Exceptions.AutoDIExceptionCtor);
                            var listAdd = Import.System.Collections.List.Add;
                            listAdd = listAdd.MakeGenericDeclaringType(Import.System.Exception);

                            processor.Emit(OpCodes.Callvirt, listAdd);

                            Instruction handlerEnd = Instruction.Create(OpCodes.Leave_S, afterCatch);
                            processor.Append(handlerEnd);

                            var exceptionHandler =
                                new ExceptionHandler(ExceptionHandlerType.Catch)
                                {
                                    CatchType = Import.System.Exception,
                                    TryStart = tryStart,
                                    TryEnd = handlerStart,
                                    HandlerStart = handlerStart,
                                    HandlerEnd = afterCatch
                                };

                            method.Body.ExceptionHandlers.Add(exceptionHandler);

                            processor.Append(afterCatch);
                            if (methodGenerator != null)
                            {
                                methodGenerator.Append("try" + Environment.NewLine + "{" + Environment.NewLine);
                                methodGenerator.Append($"    {serviceCollection.Name}.{Import.AutoDI.ServiceCollectionMixins.AddAutoDIService.Name}(typeof({importedKey.FullNameCSharp()}), typeof({registration.TargetType.FullNameCSharp()}), new Func<{Import.System.IServiceProvider.NameCSharp()}, {registration.TargetType.FullNameCSharp()}>({factoryMethod.Name}), Lifetime.{registration.Lifetime});", tryStart);
                                methodGenerator.Append(Environment.NewLine + "}" + Environment.NewLine + "catch(Exception innerException)" + Environment.NewLine + "{" + Environment.NewLine);
                                methodGenerator.Append($"    list.{listAdd.Name}(new {Import.AutoDI.Exceptions.AutoDIExceptionCtor.DeclaringType.Name}(\"Error adding type '{registration.TargetType.FullName}' with key '{registration.Key.FullName}'\", innerException));", handlerStart);
                                methodGenerator.Append(Environment.NewLine + "}" + Environment.NewLine);
                            }
                        }
                        else if (methodGenerator != null)
                        {
                            methodGenerator.Append($"{serviceCollection.Name}.{Import.AutoDI.ServiceCollectionMixins.AddAutoDIService.Name}(typeof({importedKey.FullNameCSharp()}), typeof({registration.TargetType.FullNameCSharp()}), new Func<{Import.System.IServiceProvider.NameCSharp()}, {registration.TargetType.FullNameCSharp()}>({factoryMethod.Name}), Lifetime.{registration.Lifetime});", tryStart);
                            methodGenerator.Append(Environment.NewLine);
                        }
                    }
                    catch (MultipleConstructorException e)
                    {
                        Logger.Error($"Failed to create map for {registration}\r\n{e}", null);
                    }
                    catch (Exception e)
                    {
                        Logger.Warning($"Failed to create map for {registration}\r\n{e}");
                    }
                }
            }

            Instruction @return = Instruction.Create(OpCodes.Ret);
            if (settings.DebugExceptions)
            {
                Instruction loadList = Instruction.Create(OpCodes.Ldloc, exceptionList);
                processor.Append(loadList);

                var listCount = Import.System.Collections.List.Count;
                listCount = listCount.MakeGenericDeclaringType(Import.System.Exception);
                processor.Emit(OpCodes.Callvirt, listCount);
                processor.Emit(OpCodes.Ldc_I4_0);
                processor.Emit(OpCodes.Cgt);
                processor.Emit(OpCodes.Brfalse_S, @return);

                Instruction ldStr = Instruction.Create(OpCodes.Ldstr, $"Error in {Constants.TypeName}.AddServices() generated method");
                processor.Append(ldStr);
                processor.Emit(OpCodes.Ldloc, exceptionList);

                processor.Emit(OpCodes.Newobj, Import.System.AggregateExceptionCtor);
                processor.Emit(OpCodes.Throw);

                if (methodGenerator != null)
                {
                    methodGenerator.Append("if (list.Count > 0)", loadList);
                    methodGenerator.Append(Environment.NewLine + "{" + Environment.NewLine);
                    methodGenerator.Append($"    throw new {Import.System.AggregateExceptionCtor.DeclaringType.Name}(\"Error in {Constants.TypeName}.{method.Name}() generated method\", list);", ldStr);
                    methodGenerator.Append(Environment.NewLine + "}" + Environment.NewLine);
                }
            }

            processor.Append(@return);

            method.Body.OptimizeMacros();

            return method;
        }

        private MethodDefinition GenerateFactoryMethod(TypeDefinition targetType, int index, ICodeGenerator codeGenerator)
        {
            if (!targetType.CanMapType()) return null;

            MethodDefinition targetTypeCtor = targetType.GetMappingConstructor();
            if (targetTypeCtor is null) return null;

            var factory = new MethodDefinition($"<{targetType.Name}>_generated_{index}",
                MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                ModuleDefinition.ImportReference(targetType));
            factory.Parameters.Add(new ParameterDefinition("serviceProvider", ParameterAttributes.None, Import.System.IServiceProvider));

            ILProcessor factoryProcessor = factory.Body.GetILProcessor();

            MethodReference getServiceMethod = Import.DependencyInjection.ServiceProviderServiceExtensionsGetService;

            foreach (ParameterDefinition parameter in targetTypeCtor.Parameters)
            {
                factoryProcessor.Emit(OpCodes.Ldarg_0);
                var genericGetService = new GenericInstanceMethod(getServiceMethod);
                genericGetService.GenericArguments.Add(ModuleDefinition.ImportReference(parameter.ParameterType));
                factoryProcessor.Emit(OpCodes.Call, genericGetService);
            }

            factoryProcessor.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(targetTypeCtor));
            factoryProcessor.Emit(OpCodes.Ret);

            IMethodGenerator methodGenerator = codeGenerator?.Method(factory);
            if (methodGenerator != null)
            {
                var parameters = string.Join(", ", targetTypeCtor.Parameters.Select(x => $"serviceProvider.GetService<{x.ParameterType.NameCSharp(true)}>()"));
                methodGenerator.Append($"return new {targetType.NameCSharp(true)}({parameters});", factoryProcessor.Body.Instructions.First());
                methodGenerator.Append(Environment.NewLine);
            }
            return factory;
        }

        private MethodDefinition GenerateInitMethod(MethodDefinition configureMethod, FieldDefinition globalServiceProvider)
        {
            var initMethod = new MethodDefinition(Constants.InitMethodName,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                Import.System.Void);
            var configureAction = new ParameterDefinition("configure", ParameterAttributes.None, Import.System.Action.Type.MakeGenericInstanceType(Import.AutoDI.IApplicationBuilder.Type));
            initMethod.Parameters.Add(configureAction);

            var applicationBuilder = new VariableDefinition(Import.AutoDI.IApplicationBuilder.Type);
            initMethod.Body.Variables.Add(applicationBuilder);
            ILProcessor initProcessor = initMethod.Body.GetILProcessor();

            Instruction createApplicationbuilder = Instruction.Create(OpCodes.Newobj, Import.AutoDI.ApplicationBuilder.Ctor);

            initProcessor.Emit(OpCodes.Ldsfld, globalServiceProvider);
            initProcessor.Emit(OpCodes.Brfalse_S, createApplicationbuilder);
            //Compare
            initProcessor.Emit(OpCodes.Newobj, Import.AutoDI.Exceptions.AlreadyInitializedExceptionCtor);
            initProcessor.Emit(OpCodes.Throw);

            initProcessor.Append(createApplicationbuilder);
            initProcessor.Emit(OpCodes.Stloc_0);

            initProcessor.Emit(OpCodes.Ldloc_0); //applicationBuilder
            initProcessor.Emit(OpCodes.Ldnull);
            initProcessor.Emit(OpCodes.Ldftn, configureMethod);
            initProcessor.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(Import.System.Action.Ctor.MakeGenericDeclaringType(Import.DependencyInjection.IServiceCollection)));
            initProcessor.Emit(OpCodes.Callvirt, Import.AutoDI.IApplicationBuilder.ConfigureServices);
            initProcessor.Emit(OpCodes.Pop);

            MethodDefinition setupMethod = SetupMethod.Find(ModuleDefinition, Logger);
            if (setupMethod != null)
            {
                Logger.Debug($"Found setup method '{setupMethod.FullName}'", DebugLogLevel.Default);
                initProcessor.Emit(OpCodes.Ldloc_0); //applicationBuilder
                initProcessor.Emit(OpCodes.Call, setupMethod);
                initProcessor.Emit(OpCodes.Nop);
            }
            else
            {
                Logger.Debug("No setup method found", DebugLogLevel.Default);
            }

            Instruction loadForBuild = Instruction.Create(OpCodes.Ldloc_0);

            initProcessor.Emit(OpCodes.Ldarg_0);
            initProcessor.Emit(OpCodes.Brfalse_S, loadForBuild);
            initProcessor.Emit(OpCodes.Ldarg_0);
            initProcessor.Emit(OpCodes.Ldloc_0);
            initProcessor.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(Import.System.Action.Invoke.MakeGenericDeclaringType(Import.AutoDI.IApplicationBuilder.Type)));


            initProcessor.Append(loadForBuild);
            initProcessor.Emit(OpCodes.Callvirt, Import.AutoDI.IApplicationBuilder.Build);
            initProcessor.Emit(OpCodes.Stsfld, globalServiceProvider);

            initProcessor.Emit(OpCodes.Ldsfld, globalServiceProvider);
            initProcessor.Emit(OpCodes.Call, Import.AutoDI.GlobalDI.Register);


            initProcessor.Emit(OpCodes.Ret);

            return initMethod;
        }

        private MethodDefinition GenerateDisposeMethod(FieldDefinition globalServiceProvider)
        {
            var disposeMethod = new MethodDefinition("Dispose",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                Import.System.Void);

            VariableDefinition disposable = new VariableDefinition(Import.System.IDisposable.Type);
            disposeMethod.Body.Variables.Add(disposable);

            ILProcessor processor = disposeMethod.Body.GetILProcessor();
            Instruction afterDispose = Instruction.Create(OpCodes.Nop);

            processor.Emit(OpCodes.Ldsfld, globalServiceProvider);
            processor.Emit(OpCodes.Isinst, Import.System.IDisposable.Type);
            processor.Emit(OpCodes.Dup);
            processor.Emit(OpCodes.Stloc_0); //disposable
            processor.Emit(OpCodes.Brfalse_S, afterDispose);
            processor.Emit(OpCodes.Ldloc_0); //disposable
            processor.Emit(OpCodes.Callvirt, Import.System.IDisposable.Dispose);

            processor.Append(afterDispose);

            processor.Emit(OpCodes.Ldsfld, globalServiceProvider);
            processor.Emit(OpCodes.Call, Import.AutoDI.GlobalDI.Unregister);
            processor.Emit(OpCodes.Pop);

            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Stsfld, globalServiceProvider);

            processor.Emit(OpCodes.Ret);
            return disposeMethod;
        }


    }
}