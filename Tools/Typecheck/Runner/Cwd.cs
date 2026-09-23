[NUnit.Framework.SetUpFixture]
public class Cwd { [NUnit.Framework.OneTimeSetUp] public void Go() { System.IO.Directory.SetCurrentDirectory(System.Environment.GetEnvironmentVariable("REPO_ROOT") ?? "."); } }
