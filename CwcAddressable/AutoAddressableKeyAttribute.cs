// FileName: AutoAddressableKeyAttribute.cs
using System;

namespace CwcAddressable
{
    /// <summary>
    /// 标注于 ScriptableObject 类上，声明该类资产在被标记为 Addressable 时，
    /// 需要由编辑器自动化流程静默持久化注入 Addressable 寻址 Key。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class AutoAddressableKeyAttribute : Attribute
    {
    }
}
