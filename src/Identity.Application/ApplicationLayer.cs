namespace Identity.Application;

/// <summary>
/// Represents the application layer of the system.
/// </summary>
/// <remarks>
/// This class serves as a reference for other layers when they need to access the
/// application layer assembly, via <c>typeof(ApplicationLayer).Assembly</c> — used
/// when registering MediatR handlers and AutoMapper profiles.
/// </remarks>
public class ApplicationLayer { }
