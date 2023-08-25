using System.Collections;
using System.Collections.Generic;
using FishNet.FloatingOrigin;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class Modules
{
    //this number is way bigger than the radius of the solar system
    private float huge_number = Mathf.Pow(2, 52);
    [Test]
    public void HashGridInitialization()
    {
        HashGrid<string> test = new HashGrid<string>(1024);
        Assert.NotNull(test);
    }

    [Test]
    public void FindAnyTestZero()
    {
        Debug.Log("FindAnyTestZero");
        HashGrid<string> test = new HashGrid<string>(1024);
        Assert.NotNull(test);
        string test_string = "test_value";
        Vector3d vector = Vector3d.zero;
        test.Add(vector, test_string);

        Assert.AreEqual(test_string, test.FindAnyInBoundingBox(vector, 512));
        Assert.AreEqual(test_string, test.FindAnyInBoundingBox(vector + new Vector3d(1024, 1024, 1024), 2048));
    }
    [Test]
    public void FindAnyTestNotInBox()
    {
        Debug.Log("FindAnyTestNotInBox");
        HashGrid<string> test = new HashGrid<string>(1024);
        Assert.NotNull(test);
        string test_string = "test_value";
        Vector3d vector = Vector3d.zero;
        test.Add(vector, test_string);
        Assert.IsNull(test.FindAnyInBoundingBox(vector + new Vector3d(2048, 2048, 2048), 512));
    }
    [Test]
    public void FindAnyTestBarelyInBox()
    {
        Debug.Log("FindAnyTestBarelyInBox");
        HashGrid<string> test = new HashGrid<string>(1024);
        Assert.NotNull(test);
        string test_string = "test_value";
        Vector3d vector = Vector3d.zero;
        test.Add(vector, test_string);
        Assert.AreEqual(test_string, test.FindAnyInBoundingBox(vector + new Vector3d(500, 500, 500), 512));
    }

    [Test]
    public void FindAnyExclude()
    {
        Debug.Log("FindAnyExclude");
        HashGrid<string> test = new HashGrid<string>(1024);
        Assert.NotNull(test);
        string test_string = "test_value";
        Vector3d vector = Vector3d.zero;
        test.Add(vector, test_string);

        Assert.AreEqual(null, test.FindAnyInBoundingBox(vector, 512, test_string));
        Assert.AreEqual(null, test.FindAnyInBoundingBox(vector + new Vector3d(1024, 1024, 1024), 2048, test_string));
    }
    [Test]
    public void FindAnyTestMax()
    {
        Debug.Log("FindAnyTestMax");
        HashGrid<string> test = new HashGrid<string>(1024);
        Assert.NotNull(test);
        string test_string = "test_value";
        Vector3d vector = new Vector3d(float.MaxValue, float.MaxValue, float.MaxValue);
        test.Add(vector, test_string);

        Assert.AreEqual(test_string, test.FindAnyInBoundingBox(vector, 512));
        Assert.AreEqual(test_string, test.FindAnyInBoundingBox(vector + new Vector3d(1024, 1024, 1024), 2048));
    }
    [Test]
    public void FindAnyTestMin()
    {
        Debug.Log("FindAnyTestMin");
        HashGrid<string> test = new HashGrid<string>(1024);
        Assert.NotNull(test);
        string test_string = "test_value";
        Vector3d vector = new Vector3d(float.MinValue, float.MinValue, float.MinValue);
        test.Add(vector, test_string);

        Assert.AreEqual(test_string, test.FindAnyInBoundingBox(vector, 512));
        Assert.AreEqual(test_string, test.FindAnyInBoundingBox(vector + new Vector3d(1024, 1024, 1024), 2048));
    }

    [Test]
    public void FindInBoundingBoxZero()
    {
        Debug.Log("FindInBoundingBoxZero");
        HashGrid<string> test = new HashGrid<string>(1024);
        Assert.NotNull(test);
        string test_string = "test_value";
        Vector3d vector = Vector3d.zero;
        test.Add(vector, test_string);

        Assert.True(test.FindInBoundingBox(vector, 512).Contains(test_string));
        Assert.True(test.FindAnyInBoundingBox(vector + new Vector3d(1024, 1024, 1024), 2048).Contains(test_string));
    }
    [Test]
    public void FindInBoundingBoxMax()
    {
        Debug.Log("FindInBoundingBoxMax");
        HashGrid<string> test = new HashGrid<string>(1024);
        Assert.NotNull(test);
        string test_string = "test_value";
        Vector3d vector = new Vector3d(float.MaxValue, float.MaxValue, float.MaxValue);
        test.Add(vector, test_string);

        Assert.True(test.FindInBoundingBox(vector, 512).Contains(test_string));
        Assert.True(test.FindInBoundingBox(vector + new Vector3d(1024, 1024, 1024), 2048).Contains(test_string));
    }
    [Test]
    public void FindInBoundingBoxMin()
    {
        Debug.Log("FindInBoundingBoxMin");
        HashGrid<string> test = new HashGrid<string>(1024);
        Assert.NotNull(test);
        string test_string = "test_value";
        Vector3d vector = new Vector3d(float.MinValue, float.MinValue, float.MinValue);
        test.Add(vector, test_string);

        Assert.True(test.FindInBoundingBox(vector, 512).Contains(test_string));
        Assert.True(test.FindInBoundingBox(vector + new Vector3d(1024, 1024, 1024), 2048).Contains(test_string));
    }

    [Test]
    public void BenchmarkAdd()
    {
        Debug.Log("FindInBoundingBoxMin");
        HashGrid<string> test = new HashGrid<string>(1024);


        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        for (int i = 0; i < 10000; i++)
        {
            test.Add(new Vector3d(i, i, i), "" + i);
        }
        sw.Stop();
        Debug.Log($"10000 add operations: {(sw.ElapsedMilliseconds)} ");
    }
    [Test]
    public void BenchmarkFindAny()
    {
        Debug.Log("FindInBoundingBoxMin");
        HashGrid<string> test = new HashGrid<string>(1024);

        for (int i = 0; i < 10000; i++)
        {
            test.Add(new Vector3d(i, i, i), "" + i);
        }
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        for (int i = 0; i < 10000; i++)
        {
            test.FindAnyInBoundingBox(new Vector3d(i, i, i), 1024);
        }
        sw.Stop();
        Debug.Log($"10000 find any operations: {(sw.ElapsedMilliseconds)} ");
    }
    [Test]
    public void BenchmarkFindAnySlow()
    {
        Debug.Log("FindInBoundingBoxMin");
        HashGrid<string> test = new HashGrid<string>(1024);


        var sw = new System.Diagnostics.Stopwatch();

        for (int i = 0; i < 10000; i++)
        {
            test.Add(new Vector3d(i, i, i), "" + i);
        }
        sw.Start();
        bool f = true;
        for (int i = 0; i < 10000; i++)
        {
            f = test.FindAnyInBoundingBox(new Vector3d(i, i, i), 1024).GetHashCode() % 2 == 0;
        }
        sw.Stop();
        Debug.Log(f);
        Debug.Log($"10000 find any operations no possibillity to optimize: {(sw.ElapsedMilliseconds)} ");
    }
    [Test]
    public void BenchmarkFindInBB()
    {
        Debug.Log("FindInBoundingBoxMin");
        HashGrid<string> test = new HashGrid<string>(1024);


        var sw = new System.Diagnostics.Stopwatch();

        for (int i = 0; i < 10000; i++)
        {
            test.Add(new Vector3d(i, i, i), "" + i);
        }
        sw.Start();

        for (int i = 0; i < 10000; i++)
        {
            test.FindInBoundingBox(new Vector3d(i, i, i), 1024);
        }
        sw.Stop();
        Debug.Log($"10000 find operations: {(sw.ElapsedMilliseconds)} ");

    }
    [Test]
    public void BenchmarkFindInBBSlow()
    {
        Debug.Log("FindInBoundingBoxMin");
        HashGrid<string> test = new HashGrid<string>(1024);


        var sw = new System.Diagnostics.Stopwatch();

        for (int i = 0; i < 10000; i++)
        {
            test.Add(new Vector3d(i, i, i), "" + i);
        }
        bool f = false;
        sw.Start();
        for (int i = 0; i < 10000; i++)
        {
            f = test.FindInBoundingBox(new Vector3d(i, i, i), 1024).GetHashCode() % 2 == 0;
        }
        Debug.Log(f);
        sw.Stop();
        Debug.Log($"10000 find operations no possibillity to optimize: {(sw.ElapsedMilliseconds)} ");

    }
    [Test]
    public void BenchmarkRemove()
    {
        Debug.Log("FindInBoundingBoxMin");
        HashGrid<string> test = new HashGrid<string>(1024);


        var sw = new System.Diagnostics.Stopwatch();

        for (int i = 0; i < 10000; i++)
        {
            test.Add(new Vector3d(i, i, i), "" + i);
        }
        bool f = false;
        sw.Start();

        for (int i = 0; i < 10000; i++)
        {
            test.Remove(new Vector3d(i, i, i));
        }

        Debug.Log(f);
        sw.Stop();
        Debug.Log($"10000 find operations no possibillity to optimize: {(sw.ElapsedMilliseconds)} ");

    }














}
