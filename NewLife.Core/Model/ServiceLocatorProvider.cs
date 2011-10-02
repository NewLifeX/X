namespace NewLife.Model
{
    /// <summary>
    /// This delegate type is used to provide a method that will
    /// return the current container. Used with the <see cref="ServiceLocator"/>
    /// static accessor class.
    /// </summary>
    /// <returns>An <see cref="IServiceLocator"/>.</returns>
    delegate IServiceLocator ServiceLocatorProvider();
}