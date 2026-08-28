using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "BuildLogoCard is intentionally kept as an instance member to remain consistent with the page-builder API and future theme/state injection.",
    Scope = "member",
    Target = "~M:YacaPluginSwitcher.ProfessionalMainForm.BuildLogoCard")]

[assembly: SuppressMessage(
    "Performance",
    "CA1826:Do not use Enumerable methods on indexable collections",
    Justification = "The backup collection abstraction intentionally exposes LINQ semantics; changing this call would couple the UI to the concrete collection implementation.",
    Scope = "member",
    Target = "~M:YacaPluginSwitcher.ProfessionalMainForm.RefreshHome(System.Boolean)")]
