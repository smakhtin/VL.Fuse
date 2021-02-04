﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core.Extensions;
using VL.Lib.Adaptive;
using VL.Lib.Collections;

namespace Fuse
{

    public class DrawShaderNode : AbstractShaderNode
    {
        private GpuValue<float> Output { get; set; }
        
        public DrawShaderNode(IDictionary<string,AbstractGpuValue> theVertexInputs) : base("drawShader")
        {
            Output = new GpuValue<float>("output") {ParentNode = this};
            Ins = theVertexInputs.Values;
            
        }

        protected override string SourceTemplate()
        {
            return "";
        }
    }

    public abstract class AbstractShader
    {
        public string ShaderCode { get; protected set; }

        public string ShaderName { get; protected set; }

        public AbstractShader(IDictionary<string,IDictionary<string,AbstractGpuValue>> theInputs)
        {
            
            var declarations = new HashSet<string>();
            var mixins = new HashSet<string>();
            var functionMap = new Dictionary<string, string>();

            var sourceStream = new Dictionary<string,(string source, string stream)>();
            theInputs.ForEach(input =>
            {
                HandleShader(input.Value, declarations, mixins, functionMap, out var source, out var stream);
                sourceStream.Add(input.Key,(source,stream));
            });
            
            var declarationBuilder = new StringBuilder();
            declarations.ForEach(declaration => declarationBuilder.AppendLine(declaration));
            
            var mixinBuilder = new StringBuilder();
            mixins.ForEach(mixin => mixinBuilder.Append(", " + mixin));
            
            var functionBuilder = new StringBuilder();
            functionMap?.ForEach(kv => functionBuilder.AppendLine(kv.Value));

            var templateMap = new Dictionary<string, string>
            {
                {"mixins", mixinBuilder.ToString()},
                {"declarations", declarationBuilder.ToString()},
                {"functions", functionBuilder.ToString()}
            };
            
            sourceStream.ForEach(kv =>
            {
                templateMap.Add("source" + kv.Key,kv.Value.source);
                templateMap.Add("streams" + kv.Key,kv.Value.stream);
            });
            
            // ReSharper disable once VirtualMemberCallInConstructor
            ShaderCode = ShaderTemplateEvaluator.Evaluate(Source(), templateMap);
            ShaderName = "Shader_" + Math.Abs(ShaderCode.GetHashCode());
            ShaderCode = ShaderTemplateEvaluator.Evaluate(ShaderCode, new Dictionary<string, string>{{"shaderID",ShaderName}});
        }

        public abstract string Source();
        
        protected static void HandleShader(IDictionary<string,AbstractGpuValue> theShaderInputs, ISet<string> theDeclarations, ISet<string> theMixins, Dictionary<string, string> theFunctions, out string theSource, out string theStreams)
        {
            var streamBuilder = new StringBuilder();
            theShaderInputs.ForEach(kv =>
            {
                kv.Value?.ParentNode.DeclarationList().ForEach(declaration => theDeclarations.Add(declaration));
                kv.Value?.ParentNode.MixinList().ForEach(mixin => theMixins.Add(mixin));
                kv.Value?.ParentNode.FunctionMap().ForEach(keyFunction => {if(!theFunctions.ContainsKey(keyFunction.Key))theFunctions.Add(keyFunction.Key, keyFunction.Value);});

                streamBuilder.AppendLine("        streams." + kv.Key + " = " + kv.Value.ID+";");
            });

            theSource = new DrawShaderNode(theShaderInputs).BuildSourceCode();
            theStreams = streamBuilder.ToString();
        }
    }
    
    public class DrawShader : AbstractShader
    {
        private const string DrawShaderSource = @"shader ${shaderID} : VS_PS_Base, Texturing${mixins}
{
    cbuffer Inputs{
${declarations}
    }

${functions}

    stage override void VSMain()
    {
${sourceVS}
${streamsVS}
    }

    stage override void PSMain()
    {
${sourcePS}
${streamsPS}
    }
};";

        

        public DrawShader(IDictionary<string,AbstractGpuValue> theVertexInputs, IDictionary<string,AbstractGpuValue> thePixelInputs) : base(
            new Dictionary<string, IDictionary<string, AbstractGpuValue>>
            {
                {"VS", theVertexInputs},
                {"PS", thePixelInputs}
            })
        {
            
        }


        public override string Source()
        {
            return DrawShaderSource;
        }
    }
    
    public class ComputeShader : AbstractShader
    {
        private const string ComputeShaderSource = @"shader ${shaderID} : ComputeShaderBase${mixins}
{
${declarations}

${functions}

    override void Compute()
    {
        ${sourceCS}
    }
};";
        public ComputeShader(IDictionary<string,AbstractGpuValue> theComputeInputs) : base(
            new Dictionary<string, IDictionary<string, AbstractGpuValue>>
            {
                {"CS", theComputeInputs}
            })
        {
            
        }
        
        public override string Source()
        {
            return ComputeShaderSource;
        }
    }
}