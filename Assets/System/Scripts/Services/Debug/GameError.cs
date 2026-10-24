/*
* Copyright(c) 2021  mengyu
*
* 模块名：     
* GameEntry.cs
* 
* 用途：
* 整个游戏的发生错误的枚举
*
* 作者：
* mengyu
*/

namespace Ballance2.Services.Debug
{
  /// <summary>
  /// 游戏发生错误的枚举
  /// </summary>
  public enum GameError
  {
    /// <summary>
    /// 无错误
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
  /// 错误信息
  /// </summary>
  public static class GameErrorInfo
  {
    /// <summary>
    /// 获取错误代码的说明信息
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <returns>错误代码的说明信息</returns>
    public static string GetErrorMessage(GameError err)
    {
      switch (err)
      {
        case GameError.None: return Ballance2.Services.I18N.I18N.Tr("global.error.None", "无错误。");
        case GameError.InvalidPackageName: return Ballance2.Services.I18N.I18N.Tr("global.error.InvalidPackageName", "无效的包名。");
        case GameError.OnlyCanUseInEditor: return Ballance2.Services.I18N.I18N.Tr("global.error.OnlyCanUseInEditor", "这个功能只能在Unity编辑器中使用。");
        case GameError.PrefabNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.PrefabNotFound", "未找到预制体。");
        case GameError.FileReadFailed: return Ballance2.Services.I18N.I18N.Tr("global.error.FileReadFailed", "读取文件失败。");
        case GameError.AssetNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.AssetNotFound", "未找到指定的资源。");
        case GameError.IsLoading: return Ballance2.Services.I18N.I18N.Tr("global.error.IsLoading", "正在加载，请稍后。");
        case GameError.AccessDenined: return Ballance2.Services.I18N.I18N.Tr("global.error.AccessDenined", "不能执行这个操作。");
        case GameError.ExecutionFailed: return Ballance2.Services.I18N.I18N.Tr("global.error.ExecutionFailed", "执行代码失败。");
        case GameError.NetworkError: return Ballance2.Services.I18N.I18N.Tr("global.error.NetworkError", "网络错误。");
        case GameError.RequestFailed: return Ballance2.Services.I18N.I18N.Tr("global.error.RequestFailed", "请求失败。");
        case GameError.ParamNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.ParamNotFound", "未找到参数。");
        case GameError.ParamReadOnly: return Ballance2.Services.I18N.I18N.Tr("global.error.ParamReadOnly", "参数只读。");
        case GameError.NotSupportFileType: return Ballance2.Services.I18N.I18N.Tr("global.error.NotSupportFileType", "不支持的文件格式。");
        case GameError.RegisterPackageFailed: return Ballance2.Services.I18N.I18N.Tr("global.error.RegisterPackageFailed", "注册包失败。");
        case GameError.MissingAttribute: return Ballance2.Services.I18N.I18N.Tr("global.error.MissingAttribute", "必须的配置属性丢失。");
        case GameError.PackageCanNotRun: return Ballance2.Services.I18N.I18N.Tr("global.error.PackageCanNotRun", "包不能运行。");
        case GameError.PackageIncompatible: return Ballance2.Services.I18N.I18N.Tr("global.error.PackageIncompatible", "包不兼容。");
        case GameError.ClassNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.ClassNotFound", "未找到类。");
        case GameError.FileNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.FileNotFound", "未找到文件。");
        case GameError.FunctionNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.FunctionNotFound", "未找到函数。");
        case GameError.AssetBundleNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.AssetBundleNotFound", "未找到 AssetBundle 。");
        case GameError.PackageDefNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.PackageDefNotFound", "未找到 PackageDef.xml 。");
        case GameError.NotReturn: return Ballance2.Services.I18N.I18N.Tr("global.error.NotReturn", "函数未返回参数。");
        case GameError.Empty: return Ballance2.Services.I18N.I18N.Tr("global.error.Empty", "操作为空。");
        case GameError.ParamNotProvide: return Ballance2.Services.I18N.I18N.Tr("global.error.ParamNotProvide", "参数未提供。");
        case GameError.ContextMismatch: return Ballance2.Services.I18N.I18N.Tr("global.error.ContextMismatch", "上下文无权限。");
        case GameError.NotImplemented: return Ballance2.Services.I18N.I18N.Tr("global.error.NotImplemented", "功能未实现。");
        case GameError.NotLoad: return Ballance2.Services.I18N.I18N.Tr("global.error.NotLoad", "资源未初始化。");
        case GameError.AlreadyRegistered: return Ballance2.Services.I18N.I18N.Tr("global.error.AlreadyRegistered", "指定资源已注册。");
        case GameError.NotRegister: return Ballance2.Services.I18N.I18N.Tr("global.error.NotRegister", "指定资源未注册。");
        case GameError.ConfigueNotRight: return Ballance2.Services.I18N.I18N.Tr("global.error.ConfigueNotRight", "游戏配置存在错误，这可能是当前版本存在非正式的修改，尝试下载最新版本解决问题。");
        case GameError.SystemPackageNotLoad: return Ballance2.Services.I18N.I18N.Tr("global.error.SystemPackageNotLoad", "系统包未加载。");
        case GameError.SystemNotInit: return Ballance2.Services.I18N.I18N.Tr("global.error.SystemNotInit", "系统未初始化。");
        case GameError.SystemPackageLoadFailed: return Ballance2.Services.I18N.I18N.Tr("global.error.SystemPackageLoadFailed", "系统包加载失败，这可能是当前版本存在非正式的修改，尝试下载最新版本解决问题。");
        case GameError.PackageNotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.PackageNotFound", "未找到模块包。");
        case GameError.NotFound: return Ballance2.Services.I18N.I18N.Tr("global.error.NotFound", "未找到对象。");
        case GameError.NotAvailable: return Ballance2.Services.I18N.I18N.Tr("global.error.NotAvailable", "不可用");
        default: return Ballance2.Services.I18N.I18N.TrF("global.error.Unknown", "Unknown error: {0}", err.ToString());
      }
    }
  }

}
