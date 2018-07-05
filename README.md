# OrleansCouchBaseProviders

This repository to contains a set of providers for running [Microsoft Orleans](http://github.com/dotnet/orleans) using [Couchbase](http://couchbase.com) as the storage layer.

Currently supports:

- [x] Storage Provider
- [x] Membership Provider
- [x] Fixed/sliding and dynamic document expiry per grain type
- [ ] Reminders

## How to use

The storage provider can be registered like this:

``` csharp
config.Globals.RegisterStorageProvider<Orleans.Storage.OrleansCouchBaseStorage>("default", new Dictionary<string, string>
{
    { "Server", "http://localhost:8091" },
    { "UserName", "" },
    { "Password", "" },
    { "BucketName", "default" }
});
```

Password can be left blank if the bucket is not password protected. For using multiple buckets register multiple ones with different names and then use them with the `[StorageProvider(ProviderName = "provider name")]` attribute on top of grains with state.

The membership provider can be used like this:

``` csharp
config.Globals.DeploymentId = "";
config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.Custom;
config.Globals.MembershipTableAssembly = "CouchBaseProviders";

config.Globals.RegisterStorageProvider<Orleans.Storage.OrleansCouchBaseStorage>("default", new Dictionary<string, string>
{
    { "Server", "http://localhost:8091" },
    { "UserName", "" },
    { "Password", "" },
    { "BucketName", "default" }
});
config.Globals.RegisterStorageProvider<Orleans.Storage.OrleansCouchBaseStorage>("PubSubStore", new Dictionary<string, string>
{
    { "Server", "http://localhost:8091" },
    { "UserName", "" },
    { "Password", "" },
    { "BucketName", "default" }
});
config.Globals.DataConnectionString = "http://localhost:8091";
config.PrimaryNode = null;
config.Globals.SeedNodes.Clear();
```

NOTE: The membership provider requires a bucket called `membership`.

## Document expiry per grain type

By default documents written to Couchbase will not have an expiry value set.

Support has now been added to allow expiry values to be configured per grain type.

Document expiry can be configured in two ways:

- Fixed expiry per grain type
- Dynamic expiry per grain type

### Fixed expiry per grain type

Grain expiry values are specified in the app.config file, and are applied each time a write is performed. While the expiry value is fixed, successive writes will apply the same expiry time, resulting in a sliding expiry value.

To use this feature you need to update your app.config file for your Orleans silo:

#### Add config section declaration

Add the following under the <configSections> element:

``` xml
<section name="orleans" type="CouchBaseProviders.Configuration.CouchbaseOrleansDocumentExpiry.CouchbaseOrleansConfigurationSection, CouchbaseProviders" />
```

#### Add the config section with per grain expiry values:

``` xml
<orleans>
  <grainExpiry>
    <add grainType="grainX" expiresIn="0:0:1:0"></add>
  </grainExpiry>
</orleans>
```

The expiresIn value must be a valid TimeSpan format. Examples include:

- 10 seconds: 00:00:10
- 10 minutes: 00:10:00
- 10 hours: 10:00:00
- 10 days: 10:00:00:00

Refer to the app.confg provided in the CouchBaseStorageTests project for more information.

### Dynamic expiry per grain type

Grain expiry values can be specified in the app.config file as above, but don't have to be. Instead, classes implementing either IExpiryCalculator interface or extending ExpiryCalculatorBase, can be created to calculate a suitable expiry value based on other logic, for instance model data or even calls to other grains.

This is useful where, for example, a grain holds information about a future event. If you set a fixed document expiry value, the grain state may expire and be deleted by CouchBase before the event date has been reached. Using an expiry calculator allows the storage provider to calculate the correct expiry based on the event date. 

Depending on your use cases, you may find that a grain remains active in the silo after it's grain state has been deleted by CouchBase. This can cause issues since the grain will still return it's cached state data to any callers. To prevent this situation, the ExpiringGrainBase class, which your grains can inherit from rather the Grain<TGrainState>, is configured to listen for expiry value update events (using weak events to prevent memory leaks) and set an Orleans timer (within the grain) to ensure that when the document expires, the grain deactivates. This is really only useful for grain state that is short lived. 

Important points:

- Due to the way Orleans schedules grain deactivation and CouchBase schedules purging of expired documents, you can't rely on grain deactivation being exactly in sync with document expiry in CouchBase.
- The .Net timer limitation of a maximum dueTime value of 49.7 days applies. If your expiry value is greater than this then the max value of 49.7 days will be used. This only affects grain deactivation. CouchbBase will use the correct expiry value.
- Since this is intended for short-lived grain state, an Orleans timer is used. This will not survive silo shutdowns. If you restart your silo, your grains will not remember when they're due to expire. If this doesn't work for you, then you can modify the source to use reminders, but first you should probably think about whether this makes sense or not (I doubt it does).
- If you don't need your grains to deactivate (roughly) in sync with the grain state in CouchBase then you don't need to, and shouldn't, use ExpiringGrainBase class.

#### Creating your own dynamic expiry calculators

When creating your own expiry calculator classes, implementing IExpiryCalculator is the simplest approach. Use ExpiryCalculatorBase class if you need to call other grains as part of the expiry calculation. Calling grains from a class that only implements IExpiryCalculator will result in exceptions being thrown by the storage provider.

When your expiry calculators are called, they will be passed any expiry value you have configured in your config file for the grain type the expiry calculator handles.

## How to help

Take a look at the current issues and report any issues you find.
The providers have been tested with CouchBase Community 4.1.

## License

The [MIT](LICENSE) license.
