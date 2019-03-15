# CachedRepository.NetCore
A thread-safe caching infrastructure for caching data objects, which should be requested only once when needed from the data source (db, service or any type of data source)
Please see https://github.com/oguzhankahyaoglu/CachedRepository/blob/master/README.md for actual readme file. This is the .NetCore port of this project

To Install this package:
  Install-package CachedRepository.NetCore -pre

You must register LazyCache service which is a dependency for this package: 

Add the LazyCache services in you aspnet core Startup.cs

``` CSHARP
// This method gets called by the runtime. Use this method to add services.
public void ConfigureServices(IServiceCollection services)
{
    // already existing registrations
    services.AddMvc();
    services.AddDbContext<MyContext>(options => options.UseSqlServer("some db"));
    ....

    // Register LazyCache - makes the IAppCache implementation
    // CachingService available to your code
    services.AddLazyCache();
    //Add all repository classes for Dependency Injection as Scoped variables (default)
    services.AddAllCachedRepositoriesAsServices(typeof(Application.Repositories.WorkExperienceRepo).Assembly, ServiceLifetime.Scoped);
    
}
``` 
