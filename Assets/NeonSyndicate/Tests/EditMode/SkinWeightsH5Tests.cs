using System;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class SkinWeightsH5Tests
    {
        [Test] public void AllSevenInfluencesRemainMappedAndValidated()
        {
            BoneWeight1[] data=new BoneWeight1[7];for(int i=0;i<7;i++)data[i]=new BoneWeight1{boneIndex=i,weight=i==0?.4f:.1f};
            Assert.That(SkinWeightsH5.Referenced(new byte[]{7},data,1,8),Is.EqualTo(new[]{true,true,true,true,true,true,true,false}));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void InvalidCompleteSkinCannotHideBehindAFourWeightView(int defect)
        {
            byte[] counts={1};BoneWeight1[] data={new BoneWeight1{boneIndex=0,weight=1}};int vertices=1;
            if(defect==0)data[0].weight=float.NaN;
            if(defect==1)data[0].weight=-1;
            if(defect==2)data[0].weight=.9f;
            if(defect==3)data[0].boneIndex=3;
            if(defect==4)counts[0]=0;
            if(defect==5)vertices=2;
            if(defect==6)data=new[]{data[0],data[0]};
            Assert.Throws<ArgumentException>(()=>SkinWeightsH5.Referenced(counts,data,vertices,2));
        }
    }
}
