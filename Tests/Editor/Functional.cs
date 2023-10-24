using FishNet.FloatingOrigin;
using NUnit.Framework;
using UnityEngine;

public class Functional
{
    //this number is way bigger than the radius of the solar system
    private float huge_number = Mathf.Pow(2, 52);
    // A Test behaves as an ordinary method
    [Test]
    public void TestZeroPositiveUnity()
    {
        Vector3 unityPosition = new Vector3(10000, 10000, 10000);
        Vector3d offset = ((Vector3d)Vector3.zero);
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(realPosition, Functions.UnityToReal(unityPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }
    [Test]
    public void MaxDistanceBenchmark()
    {
        for (int i = 0; i < 100000; i++)
        {
            Functions.MaxLengthScalar(new Vector3d(i, i, i));
        }
    }
    [Test]
    public void Vector3dDistanceBenchmark()
    {
        for (int i = 0; i < 100000; i++)
        {
            Vector3d.Distance(new Vector3d(i, i, i), Vector3d.zero);
        }
    }

    [Test]
    public void TestZeroNegativeUnity()
    {
        Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
        Vector3d offset = ((Vector3d)Vector3.zero);
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(realPosition, Functions.UnityToReal(unityPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }

    [Test]
    public void TestMaxPositiveUnity()
    {
        Vector3 unityPosition = new Vector3(10000, 10000, 10000);
        Vector3d offset = ((Vector3d)new Vector3(huge_number, huge_number, huge_number));
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(realPosition, Functions.UnityToReal(unityPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }

    [Test]
    public void TestMaxNegativeUnity()
    {
        Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
        Vector3d offset = ((Vector3d)new Vector3(huge_number, huge_number, huge_number));
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(realPosition, Functions.UnityToReal(unityPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }

    [Test]
    public void TestMinPositiveUnity()
    {
        Vector3 unityPosition = new Vector3(10000, 10000, 10000);
        Vector3d offset = ((Vector3d)new Vector3(-huge_number, -huge_number, -huge_number));
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(realPosition, Functions.UnityToReal(unityPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }

    [Test]
    public void TestMinNegativeUnity()
    {
        Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
        Vector3d offset = ((Vector3d)new Vector3(-huge_number, -huge_number, -huge_number));
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(realPosition, Functions.UnityToReal(unityPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }



    [Test]
    public void TestZeroPositiveUnityRealToUnity()
    {
        Vector3 unityPosition = new Vector3(10000, 10000, 10000);
        Vector3d offset = ((Vector3d)Vector3.zero);
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(unityPosition, Functions.RealToUnity(realPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }

    [Test]
    public void TestZeroNegativeUnityRealToUnity()
    {
        Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
        Vector3d offset = ((Vector3d)Vector3.zero);
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(unityPosition, Functions.RealToUnity(realPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }

    [Test]
    public void TestMaxPositiveUnityRealToUnity()
    {
        Vector3 unityPosition = new Vector3(10000, 10000, 10000);
        Vector3d offset = ((Vector3d)new Vector3(huge_number, huge_number, huge_number));
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(unityPosition, Functions.RealToUnity(realPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }

    [Test]
    public void TestMaxNegativeUnityRealToUnity()
    {
        Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
        Vector3d offset = ((Vector3d)new Vector3(huge_number, huge_number, huge_number));
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(unityPosition, Functions.RealToUnity(realPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }

    [Test]
    public void TestMinPositiveUnityRealToUnity()
    {
        Vector3 unityPosition = new Vector3(10000, 10000, 10000);
        Vector3d offset = ((Vector3d)new Vector3(-huge_number, -huge_number, -huge_number));
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(unityPosition, Functions.RealToUnity(realPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }

    [Test]
    public void TestMinNegativeUnityRealToUnity()
    {
        Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
        Vector3d offset = ((Vector3d)new Vector3(-huge_number, -huge_number, -huge_number));
        Vector3d realPosition = ((Vector3d)unityPosition) + offset;

        Assert.AreEqual(unityPosition, Functions.RealToUnity(realPosition, offset));
        Assert.AreNotEqual(realPosition, offset);
    }

}
