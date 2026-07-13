namespace Kiln.Studio.ViewModels;

/// <summary>
/// The six persistent top-level destinations in the left navigation rail (ADR-054/PLAN-072).
/// Menus, Theme, and Deployment are placeholder-only ("coming soon") in this iteration.
/// </summary>
public enum NavTarget
{
    Content,
    Assets,
    Menus,
    Theme,
    Deployment,
    Settings,
}
