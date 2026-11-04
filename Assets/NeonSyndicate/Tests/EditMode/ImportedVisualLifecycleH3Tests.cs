using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
namespace NeonSyndicate.Tests
{
    public sealed class ImportedVisualLifecycleH3Tests
    {
        [Test]
        public void ComponentConstructionDoesNotAllocateNativePresentationObjects()
        {
            Dictionary<short,OpCode> codes=new Dictionary<short,OpCode>();
            foreach(FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static))
            {OpCode code=(OpCode)field.GetValue(null);codes[code.Value]=code;}
            ConstructorInfo constructor=typeof(ImportedVisual04OD).GetConstructor(Type.EmptyTypes);
            Assert.That(constructor,Is.Not.Null);
            byte[] body=constructor.GetMethodBody().GetILAsByteArray();
            List<string> nativeCalls=new List<string>();
            for(int at=0;at<body.Length;)
            {
                short value=body[at++];if(value==0xfe)value=(short)(0xfe00|body[at++]);
                OpCode code=codes[value];int bytes=0;
                switch(code.OperandType)
                {
                    case OperandType.InlineNone:break;
                    case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:bytes=1;break;
                    case OperandType.InlineVar:bytes=2;break;
                    case OperandType.InlineI8:case OperandType.InlineR:bytes=8;break;
                    case OperandType.InlineSwitch:bytes=4+BitConverter.ToInt32(body,at)*4;break;
                    default:bytes=4;break;
                }
                if(code.OperandType==OperandType.InlineMethod)
                {
                    MethodBase called=constructor.Module.ResolveMethod(BitConverter.ToInt32(body,at));
                    if(called.DeclaringType.Namespace=="UnityEngine" && called.DeclaringType!=typeof(UnityEngine.MonoBehaviour))nativeCalls.Add(called.DeclaringType.Name+"."+called.Name);
                }
                at+=bytes;
            }
            Assert.That(nativeCalls,Is.Empty,"Unity native construction belongs in Start/Initialize, never a component field initializer.");
        }
    }
}
