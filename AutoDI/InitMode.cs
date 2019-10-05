namespace AutoDI
{
    /// <summary>
    /// Controls when AutoDI is initialized
    /// </summary>
    public enum InitMode
    {
        /// <summary>
        /// The container must be initialized manually.
        /// </summary>
        None,
        /// <summary>
        /// The container will be initialized on your program's entry point.
        /// Recommended for executable programs.
        /// </summary>
        EntryPoint,
        /// <summary>
        /// The container will be initialized on assembly module load.
        /// Recommended for libraries.
        /// </summary>
        ModuleLoad
    }
}
