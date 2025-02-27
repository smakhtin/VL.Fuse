﻿using Stride.Rendering.Materials;

namespace Fuse.function;

public interface IFunctionParameter : IComputeNode
{
    string TypeName();

    string ID { get; }

    InputModifier Modifier { get; }

    string ModifierString();

    int ArgumentNumber { get; }
    uint HashCode { get; set; }
}