using System;
using AutoDI;

namespace AssemblyToProcess
{
    public partial class ClassWithNullParam
    {
        public IService Service { get; }

        partial void Resolve<T>(ref T service);

        public ClassWithNullParam( [Dependency( (object)null )] IService service = null )
        {
            Resolve(ref service);
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public partial class ClassWithNullParam
    {
        partial void Resolve<T>(ref T service)
        {
            if (service == null) GlobalDI.GetService<T>();
        }
    }

    public class ClassWithStringParam
    {
        public IService Service { get; }

        public ClassWithStringParam( [Dependency( "Test String" )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithIntParam
    {
        public IService Service { get; }

        public ClassWithIntParam( [Dependency( 42 )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithLongParam
    {
        public IService Service { get; }

        public ClassWithLongParam( [Dependency( long.MaxValue )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithDoubleParam
    {
        public IService Service { get; }

        public ClassWithDoubleParam( [Dependency( double.NaN )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithFloatParam
    {
        public IService Service { get; }

        public ClassWithFloatParam( [Dependency( float.MinValue )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithShortParam
    {
        public IService Service { get; }

        public ClassWithShortParam( [Dependency( short.MinValue )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithByteParam
    {
        public IService Service { get; }

        public ClassWithByteParam( [Dependency( byte.MaxValue )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithUnsignedIntParam
    {
        public IService Service { get; }

        public ClassWithUnsignedIntParam( [Dependency( (uint)int.MaxValue + 1 )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithUnsignedLongParam
    {
        public IService Service { get; }

        public ClassWithUnsignedLongParam( [Dependency( (ulong)long.MaxValue + 1 )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithUnsignedShortParam
    {
        public IService Service { get; }

        public ClassWithUnsignedShortParam( [Dependency( (ushort)short.MaxValue + 1 )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithEnumParam
    {
        public IService Service { get; }

        public ClassWithEnumParam( [Dependency( EnumParam.BeAwesome )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithSignedByteParam
    {
        public IService Service { get; }

        public ClassWithSignedByteParam( [Dependency( sbyte.MinValue )] IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public class ClassWithTwoDependencyParams
    {
        public IService Service { get; }

        public ClassWithTwoDependencyParams( [Dependency( 4, "Test" )]IService service = null )
        {
            Service = service ?? throw new ArgumentNullException( nameof( service ) );
        }
    }

    public enum EnumParam : byte
    {
        None,
        BeAwesome,
        Everything
    }
}