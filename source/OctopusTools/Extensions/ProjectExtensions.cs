using System;
using System.Collections.Generic;
using System.Linq;
using OctopusTools.Client;
using OctopusTools.Model;

// ReSharper disable CheckNamespace
public static class ProjectExtensions
// ReSharper restore CheckNamespace
{
    public static Project GetProject(this IOctopusSession session, string projectName)
    {
        var projects = session.List<Project>(session.RootDocument.Link("Projects"));

        var project = projects.FirstOrDefault(x => string.Equals(x.Name, projectName, StringComparison.InvariantCultureIgnoreCase));
        if (project == null)
        {
            throw new ArgumentException(string.Format("A project named '{0}' could not be found.", projectName));
        }

        return project;
    }

    public static IList<Step> FindStepsForProject(this IOctopusSession session, Project project)
    {
        return session.List<Step>(project.Link("Steps"));
    }

	public static SelectedPackage GetPackageForStep(this IOctopusSession session, Step step, string version)
	{
		var versions = session.List<PackageVersion>(step.Link("AvailablePackageVersions"));

		var latest = versions.FirstOrDefault(s => s.Version == version);
		if (latest == null)
		{
			throw new Exception(string.Format("There are no available packages named '{0}' with a version '{1}'.", step.NuGetPackageId, version));
		}

		return new SelectedPackage { StepId = step.Id, NuGetPackageVersion = latest.Version };
	}

    public static SelectedPackage GetLatestPackageForStep(this IOctopusSession session, Step step)
    {
        var versions = session.List<PackageVersion>(step.Link("AvailablePackageVersions"));

        var latest = versions.FirstOrDefault();
        if (latest == null)
        {
            throw new Exception(string.Format("There are no available packages named '{0}'.", step.NuGetPackageId));
        }

        return new SelectedPackage { StepId = step.Id, NuGetPackageVersion = latest.Version };
    }

    public static Release CreateRelease(this IOctopusSession session, Project project, IList<SelectedPackage> latestVersions, string version, string releaseNotes)
    {
        var release = new Release();
        release.Assembled = DateTimeOffset.UtcNow;
        release.AssembledBy = Environment.UserName;
        release.Version = version;
        release.SelectedPackages = latestVersions.ToArray();
        release.ReleaseNotes = releaseNotes ?? string.Empty;

        return session.Create(project.Link("Releases"), release);
    }

    public static Release GetRelease(this IOctopusSession session, Project project, string version)
    {
        var releases = session.List<Release>(project.Link("Releases"));

        var release = releases.FirstOrDefault(x => string.Equals(x.Version, version, StringComparison.InvariantCultureIgnoreCase));

        return release;
    }

    public static IList<Release> GetReleases(this IOctopusSession session, Project project, int skip, int take)
    {
        return session.List<Release>(project.Link("Releases"), new QueryString { { "skip", skip }, { "take", take}});
    }


    public static Release GetLatestRelease(this IOctopusSession session, Project project)
    {
        return session.GetReleases(project, 0, 1).OrderByDescending(p => SemanticVersion.Parse(p.Version)).First();
    }


    public static IEnumerable<Deployment> GetMostRecentDeployments(this IOctopusSession session, Project project)
    {
        return session.List<Deployment>(project.Link(("RecentDeployments")));
    }
}
