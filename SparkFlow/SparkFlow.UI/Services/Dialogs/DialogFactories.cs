namespace SparkFlow.UI.Services.Dialogs;

// ✅ Strongly-typed factories so DI can distinguish them.
public delegate object HealthDialogContentFactory(string profileId);
public delegate object GameDialogContentFactory(string profileId);