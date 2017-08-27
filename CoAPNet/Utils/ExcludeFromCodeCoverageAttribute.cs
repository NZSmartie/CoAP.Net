using System;

#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5
    
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public class ExcludeFromCodeCoverageAttribute : Attribute
    { }
}

#endif