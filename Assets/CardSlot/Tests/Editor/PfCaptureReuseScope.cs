using System;
using System.Reflection;
using NUnit.Framework;

/// <summary>
/// One render per capture fixture per EditMode run.
///
/// <para>A capture fixture declares an end state, and two renders of unchanged inputs are the same image,
/// so a fixture that several classes render (the harness's RunAll test, a surface's floor class) is drawn
/// once and answered from that render afterwards. Measured on a SKU: every shipped fixture was drawn at
/// least twice per run, and the capture classes were 91% of a ~300 s run. <c>Tools/pf/pf-tests</c> opens
/// the same scope itself; this fixture covers the Test Runner window and CI.</para>
///
/// <para>The harness renders again whenever an input moved: a changed fixture, any imported asset, an
/// overwritten output file, or the same fixture asked for twice in a row (so a determinism test that runs
/// one fixture twice still compares two real renders). A test that changes an input in memory without
/// saving it calls <c>CaptureHarness.RunFresh</c>.</para>
///
/// <para>In the global namespace on purpose: an NUnit set-up fixture wraps only the fixtures in its own
/// namespace, and this one must wrap them all. Found by name, so this assembly needs no reference to the
/// framework's Editor assembly. Emitted by the Setup Wizard's test kit. This file is the SKU's.</para>
/// </summary>
[SetUpFixture]
public sealed class PfCaptureReuseScope
{
    private IDisposable _scope;

    [OneTimeSetUp]
    public void Open()
    {
        var harness = Type.GetType("ClassicSpins.PrototypeFramework.Editor.Capture.CaptureHarness, ClassicSpins.PrototypeFramework.Editor");
        var open = harness?.GetMethod("ReuseCapturesWithinRun", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
        _scope = open?.Invoke(null, null) as IDisposable;
    }

    [OneTimeTearDown]
    public void Close()
    {
        _scope?.Dispose();
        _scope = null;
    }
}
