/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameEntry.cs
* 
* Purpose:
* Game error codes.
*
* Author:
* mengyu
*/

namespace Ballance2.Services.Debug
{
  /// <summary>
  /// Game error codes.
  /// </summary>
  public enum GameError
  {
    /// <summary>
    /// No error.
    /// </summary>
    None,
    
    UnKnow,
    
    AlreadyRegistered,
    
    NotRegister,
    
    NotLoad,
    
    NotFound,
    
    ConfigueNotRight,
    
    NotImplemented,
    
    ContextMismatch,
    
    ParamNotProvide,
    
    ParamNotFound,
    
    ParamReadOnly,
    
    Empty,
    
    MissingAttribute,
    
    PackageCanNotRun,
    
    PackageIncompatible,
    
    ClassNotFound,
    
    FileNotFound,
    
    FunctionNotFound,
    
    PackageDefNotFound,
    
    AssetBundleNotFound,
    
    AssetNotFound,
    
    FileReadFailed,
    
    NotReturn,
    
    InvalidPackageName,
    
    RegisterPackageFailed,
    
    NotSupportFileType,
    
    NetworkError,
    
    ExecutionFailed,
    
    AccessDenined,
    
    IsLoading,
    
    SystemPackageNotLoad,
    
    SystemPackageLoadFailed,
    
    SystemNotInit,
    
    UnKnowType,
    
    EnvBindCheckFailed,
    
    OnlyCanUseInEditor,
    
    PrefabNotFound,
    
    PackageNotFound,
    
    NotAvailable,
    
    RequestFailed,
  }

  /// <summary>
  /// Error message.
  /// </summary>
  public static class GameErrorInfo
  {
    /// <summary>
    /// Get the description of an error code.
    /// </summary>
    /// <param name="err">Error code.</param>
    /// <returns>Error code description.</returns>
    public static string GetErrorMessage(GameError err)
    {
      switch (err)
      {
        case GameError.None: return Ballance2.Services.I18N.I18N.Tr("global.error.None", "No error.");
        case GameError.InvalidPackageName: return Ballance2.Services.I18N.I18N.Tr("global.error.InvalidPackageName", "Invalid package name.");
        case GameError.OnlyCanUseInEditor: return Ballance2.Services.I18N.I18N.Tr("global.error.OnlyCanUseInEditor", "This feature is available only in the Unity editor.");
        case GameError.PrefabNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.PrefabNotFound", "Prefab not found.");
        case GameError.FileReadFailed: return Ballance2.Services.I18N.I18N.Tr("global.error.FileReadFailed", "Failed to read file.");
        case GameError.AssetNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.AssetNotFound", "The specified resource was not found.");
        case GameError.IsLoading: return Ballance2.Services.I18N.I18N.Tr("global.error.IsLoading", "Loading, please wait.");
        case GameError.AccessDenined: return Ballance2.Services.I18N.I18N.Tr("global.error.AccessDenined", "This operation is not allowed.");
        case GameError.ExecutionFailed: return Ballance2.Services.I18N.I18N.Tr("global.error.ExecutionFailed", "Code execution failed.");
        case GameError.NetworkError: return Ballance2.Services.I18N.I18N.Tr("global.error.NetworkError", "Network error.");
        case GameError.RequestFailed: return Ballance2.Services.I18N.I18N.Tr("global.error.RequestFailed", "Request failed.");
        case GameError.ParamNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.ParamNotFound", "Parameter not found.");
        case GameError.ParamReadOnly: return Ballance2.Services.I18N.I18N.Tr("global.error.ParamReadOnly", "Parameter is read-only.");
        case GameError.NotSupportFileType: return Ballance2.Services.I18N.I18N.Tr("global.error.NotSupportFileType", "Unsupported file format.");
        case GameError.RegisterPackageFailed: return Ballance2.Services.I18N.I18N.Tr("global.error.RegisterPackageFailed", "Failed to register package.");
        case GameError.MissingAttribute: return Ballance2.Services.I18N.I18N.Tr("global.error.MissingAttribute", "Required configuration property is missing.");
        case GameError.PackageCanNotRun: return Ballance2.Services.I18N.I18N.Tr("global.error.PackageCanNotRun", "Package cannot run.");
        case GameError.PackageIncompatible: return Ballance2.Services.I18N.I18N.Tr("global.error.PackageIncompatible", "Package is incompatible.");
        case GameError.ClassNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.ClassNotFound", "Class not found.");
        case GameError.FileNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.FileNotFound", "File not found.");
        case GameError.FunctionNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.FunctionNotFound", "Function not found.");
        case GameError.AssetBundleNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.AssetBundleNotFound", "AssetBundle not found.");
        case GameError.PackageDefNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.PackageDefNotFound", "PackageDef.xml not found.");
        case GameError.NotReturn: return Ballance2.Services.I18N.I18N.Tr("global.error.NotReturn", "Function did not return a value.");
        case GameError.Empty: return Ballance2.Services.I18N.I18N.Tr("global.error.Empty", "Operation is empty.");
        case GameError.ParamNotProvide: return Ballance2.Services.I18N.I18N.Tr("global.error.ParamNotProvide", "Parameter was not provided.");
        case GameError.ContextMismatch: return Ballance2.Services.I18N.I18N.Tr("global.error.ContextMismatch", "Context does not have permission.");
        case GameError.NotImplemented: return Ballance2.Services.I18N.I18N.Tr("global.error.NotImplemented", "Feature is not implemented.");
        case GameError.NotLoad: return Ballance2.Services.I18N.I18N.Tr("global.error.NotLoad", "Resource is not initialized.");
        case GameError.AlreadyRegistered: return Ballance2.Services.I18N.I18N.Tr("global.error.AlreadyRegistered", "The specified resource is already registered.");
        case GameError.NotRegister: return Ballance2.Services.I18N.I18N.Tr("global.error.NotRegister", "The specified resource is not registered.");
        case GameError.ConfigueNotRight: return Ballance2.Services.I18N.I18N.Tr("global.error.ConfigueNotRight", "Game configuration is invalid. The installation may have unofficial modifications; try downloading the latest version.");
        case GameError.SystemPackageNotLoad: return Ballance2.Services.I18N.I18N.Tr("global.error.SystemPackageNotLoad", "System package is not loaded.");
        case GameError.SystemNotInit: return Ballance2.Services.I18N.I18N.Tr("global.error.SystemNotInit", "System is not initialized.");
        case GameError.SystemPackageLoadFailed: return Ballance2.Services.I18N.I18N.Tr("global.error.SystemPackageLoadFailed", "System package failed to load. The installation may have unofficial modifications; try downloading the latest version.");
        case GameError.PackageNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.PackageNotFound", "Package not found.");
        case GameError.NotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.NotFound", "Object not found.");
        case GameError.NotAvailable: return Ballance2.Services.I18N.I18N.Tr("global.error.NotAvailable", "Unavailable");
        default: return Ballance2.Services.I18N.I18N.TrF("global.error.Unknown", "Unknown error: {0}", err.ToString());
      }
    }
  }

}
