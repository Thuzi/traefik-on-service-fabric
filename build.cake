#addin "nuget:?package=Newtonsoft.Json&version=9.0.1"
#addin "nuget:?package=Cake.Json&version=3.0.0"
#tool "nuget:?package=OctopusTools&version=4.24.4"
#tool "nuget:?package=GitVersion.CommandLine&Version=4.0.0"
#addin "nuget:?package=Cake.FileHelpers&version=3.1.0"
#addin "nuget:?package=Cake.Git&version=0.19.0"

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");
var sfPackageRoot = "./src/Traefik/";

Task("Clean")
    .WithCriteria(c => HasArgument("rebuild"))
    .Does(() =>
{
    //CleanDirectory($"./src/Example/bin/{configuration}");
});

Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{

    MSBuild($"{sfPackageRoot}Traefik.sfproj", new MSBuildSettings{
        Configuration = configuration,
        ToolVersion = MSBuildToolVersion.VS2019
    }.WithTarget("Package"));
    EnsureDirectoryExists( $"{sfPackageRoot}pkg/Release/PublishProfiles/");
    CopyFiles($"{sfPackageRoot}PublishProfiles/*.xml", $"{sfPackageRoot}pkg/Release/PublishProfiles/");
    EnsureDirectoryExists( $"{sfPackageRoot}pkg/Release/ApplicationParameters/");
    CopyFiles($"{sfPackageRoot}ApplicationParameters/*.xml", $"{sfPackageRoot}pkg/Release/ApplicationParameters/");
});

GitVersion semVer = null;
Task("Apply Version")
	.WithCriteria(EnvironmentVariable("GH_UNAME") != null && EnvironmentVariable("GH_PASSWORD") != null) 
	.Does(() =>
	{
		
		var result = GitVersion(new GitVersionSettings
		{
			
			UserName = EnvironmentVariable("GH_UNAME"),
        	Password = EnvironmentVariable("GH_PASSWORD")

		});
		
		semVer = result;
		//FileWriteText("./version.properties", $"LAST_RELEASED_TAG={semVer.MajorMinorPatch}");

		//update the package.json file at root for versioning.
		var jObj = ParseJsonFromFile("./package.json");
		jObj.Property("version").Value = semVer.MajorMinorPatch;
		SerializeJsonToPrettyFile("./package.json", jObj);

		Information(semVer.MajorMinorPatch);
	});


Task("Package")
    .IsDependentOn("Build")
    .IsDependentOn("Apply Version")
    .Does(() =>
{
    EnsureDirectoryExists("./artifacts/ServiceFabric");
    CopyDirectory($"{sfPackageRoot}pkg/Release", "./artifacts/ServiceFabric");

    UpdateServiceManifest(File($"./artifacts/ServiceFabric/TraefikPkg/ServiceManifest.xml"));
    UpdateAppManifest(File("./artifacts/ServiceFabric/ApplicationManifest.xml"));

    OctoPack("ServiceFabric.Traefik", new OctopusPackSettings
			{
				Version = semVer.MajorMinorPatch,
				BasePath = "./artifacts/ServiceFabric",
				OutFolder = Directory("./artifacts/microservices"),
				Format = OctopusPackFormat.Zip,
				Overwrite = true
			});
});


Task("Release")
    .WithCriteria(EnvironmentVariable("GH_UNAME") != null 
					&& EnvironmentVariable("GH_PASSWORD") != null 
					&& EnvironmentVariable("OCTO_API_KEY") != null 
					&& EnvironmentVariable("NUGET_API") != null)
	.IsDependentOn("Apply Version")
	.IsDependentOn("Package")
    .Does(() =>
{
    	foreach (var package in GetFiles("./artifacts/microservices/*." + semVer.MajorMinorPatch + "*.*"))
		{
			Information($"Publishing Package {package}");
			OctoPush("https://octo.thuzi.com/", 
				EnvironmentVariable("OCTO_API_KEY"), 
				package, 
				new OctopusPushSettings
				{
					ReplaceExisting = true
				});
		}

		OctoCreateRelease("ServiceFabric - Traefik", new CreateReleaseSettings
		{
			Server = "https://octo.thuzi.com/",
			ApiKey = EnvironmentVariable("OCTO_API_KEY"),
			ReleaseNumber = semVer.MajorMinorPatch,
			DeploymentProgress = false,
			DeployTo ="Events Platform - Staging",
			WaitForDeployment = false,
		});

		Information($"\tTagging this build: "+semVer.MajorMinorPatch);
		GitTag(".", semVer.MajorMinorPatch);
		Information($"\tPushing the tag: "+semVer.MajorMinorPatch);
		GitPushRef(".", EnvironmentVariable("GH_UNAME"), EnvironmentVariable("GH_PASSWORD"), "origin", semVer.MajorMinorPatch);
});


void UpdateServiceManifest(FilePath file)
{
	XmlPoke(file, "/x:ServiceManifest/x:CodePackage[@Name=\"Code\"]/@Version", 
		semVer.MajorMinorPatch, 
		new XmlPokeSettings
		{
			Namespaces = new Dictionary<string, string>
			{
				{ "x", "http://schemas.microsoft.com/2011/01/fabric" }
			}
		});

	XmlPoke(file, "/x:ServiceManifest/@Version", 
		semVer.MajorMinorPatch, 
		new XmlPokeSettings
		{
			Namespaces = new Dictionary<string, string>
			{
				{ "x", "http://schemas.microsoft.com/2011/01/fabric" }
			}
		});
}

void UpdateAppManifest(FilePath file)
{
	XmlPoke(file, 
		"/x:ApplicationManifest/@ApplicationTypeVersion", 
		semVer.MajorMinorPatch, 
		new XmlPokeSettings
		{
			Namespaces = new Dictionary<string, string>
			{
				{ "x", "http://schemas.microsoft.com/2011/01/fabric" }
			}
		});

	
		XmlPoke(file, 
			$"//x:ServiceManifestRef[@ServiceManifestName=\"TraefikPkg\"]/@ServiceManifestVersion", 
			semVer.MajorMinorPatch, 
			new XmlPokeSettings
			{
				Namespaces = new Dictionary<string, string>
				{
					{ "x", "http://schemas.microsoft.com/2011/01/fabric" }
				}
			});
	
}


RunTarget(target);
