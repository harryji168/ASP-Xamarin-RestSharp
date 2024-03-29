﻿//   Copyright © 2009-2021 John Sheehan, Andrew Young, Alexey Zimarev and RestSharp community
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 

// ReSharper disable once CheckNamespace
namespace RestSharp.Serializers;

/// <summary>
/// Allows control how class and property names and values are deserialized by XmlAttributeDeserializer
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = false)]
public sealed class DeserializeAsAttribute : Attribute {
    /// <summary>
    /// The name to use for the serialized element
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Sets if the property to Deserialize is an Attribute or Element (Default: false)
    /// </summary>
    public bool Attribute { get; set; }

    /// <summary>
    /// Sets if the property to Deserialize is a content of current Element (Default: false)
    /// </summary>
    public bool Content { get; set; }
}