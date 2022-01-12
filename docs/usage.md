---
title: Usage
---

## Recommended Usage

RestSharp works best as the foundation for a proxy class for your API. Here are a couple of examples from the <a href="http://github.com/twilio/twilio-csharp">Twilio</a> library.

Create a class to contain your API proxy implementation with an `ExecuteAsync` (or any of the extensions) method for funneling all requests to the API. 
This allows you to set commonly-used parameters and other settings (like authentication) shared across requests. 
Because an account ID and secret key are required for every request you are required to pass those two values when 
creating a new instance of the proxy. 

::: warning
Note that exceptions from `ExecuteAsync` are not thrown but are available in the `ErrorException` property.
:::

```csharp
// TwilioApi.cs
public class TwilioApi {
    const string BaseUrl = "https://api.twilio.com/2008-08-01";

    readonly RestClient _client;

    string _accountSid;

    public TwilioApi(string accountSid, string secretKey) {
        _client = new RestClient(BaseUrl);
        _client.Authenticator = new HttpBasicAuthenticator(accountSid, secretKey);
        _client.AddDefaultParameter(
            "AccountSid", _accountSid, ParameterType.UrlSegment
        ); // used on every request
        _accountSid = accountSid;
    }
}
```

Next, define a class that maps to the data returned by the API.

```csharp
// Call.cs
public class Call {
    public string Sid { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
    public string CallSegmentSid { get; set; }
    public string AccountSid { get; set; }
    public string Called { get; set; }
    public string Caller { get; set; }
    public string PhoneNumberSid { get; set; }
    public int Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Duration { get; set; }
    public decimal Price { get; set; }
    public int Flags { get; set; }
}
```

Then add a method to query the API for the details of a specific Call resource.

```csharp
// TwilioApi.cs, GetCall method of TwilioApi class
public Task<Call> GetCall(string callSid) {
    var request = new RestRequest("Accounts/{AccountSid}/Calls/{CallSid}");
    request.RootElement = "Call";
    request.AddParameter("CallSid", callSid, ParameterType.UrlSegment);

    return _client.GetAsync<Call>(request);
}
```

There's some magic here that RestSharp takes care of, so you don't have to.

The API returns XML, which is automatically detected and deserialized to the Call object using the default `XmlDeserializer`.
By default, a call is made via a GET HTTP request. You can change this by setting the `Method` property of `RestRequest` 
or specifying the method in the constructor when creating an instance (covered below).
Parameters of type `UrlSegment` have their values injected into the URL based on a matching token name existing in the Resource property value. 
`AccountSid` is set in `TwilioApi.Execute` because it is common to every request.
We specify the name of the root element to start deserializing from. In this case, the XML returned is `<Response><Call>...</Call></Response>` and since the Response element itself does not contain any information relevant to our model, we start the deserializing one step down the tree.

You can also make POST (and PUT/DELETE/HEAD/OPTIONS) requests:

```csharp
// TwilioApi.cs, method of TwilioApi class
public Task<Call> InitiateOutboundCall(CallOptions options) {
    Require.Argument("Caller", options.Caller);
    Require.Argument("Called", options.Called);
    Require.Argument("Url", options.Url);

    var request = new RestRequest("Accounts/{AccountSid}/Calls");
    request.RootElement = "Calls";

    request.AddParameter("Caller", options.Caller);
    request.AddParameter("Called", options.Called);
    request.AddParameter("Url", options.Url);

    if (options.Method.HasValue) request.AddParameter("Method", options.Method);
    if (options.SendDigits.HasValue()) request.AddParameter("SendDigits", options.SendDigits);
    if (options.IfMachine.HasValue) request.AddParameter("IfMachine", options.IfMachine.Value);
    if (options.Timeout.HasValue) request.AddParameter("Timeout", options.Timeout.Value);

    return _client.PostAsync<Call>(request);
}
```

This example also demonstrates RestSharp's lightweight validation helpers. 
These helpers allow you to verify before making the request that the values submitted are valid. 
Read more about Validation here.

All the values added via `AddParameter` in this example will be submitted as a standard encoded form, 
similar to a form submission made via a web page. If this were a GET-style request (GET/DELETE/OPTIONS/HEAD), 
the parameter values would be submitted via the query string instead. You can also add header and cookie 
parameters with `AddParameter`. To add all properties for an object as parameters, use `AddObject`. 
To add a file for upload, use `AddFile` (the request will be sent as a multipart encoded form). 
To include a request body like XML or JSON, use `AddXmlBody` or `AddJsonBody`.

## Request Parameters

After you've created a `RestRequest`, you can add parameters to it.
Here is a Description of the 5 currently supported types and their behavior when using the default IHttp implementation.

### Http Header

Adds the parameter as an HTTP header that is sent along with the request. The header name is the name of the parameter and the header value is the value.

### Get or Post

This behaves differently based on the method. If you execute a GET call, RestSharp will append the parameters to the Url in the form `url?name1=value1&name2=value2`.

On a POST or PUT Requests, it depends on whether you have files attached to a Request.
If not, the Parameters will be sent as the body of the request in the form `name1=value1&name2=value2`. Also, the request will be sent as `application/x-www-form-urlencoded`.

In both cases, name and value will automatically be url-encoded.

If you have files, RestSharp will send a `multipart/form-data` request. Your parameters will be part of this request in the form:

```
Content-Disposition: form-data; name="parameterName"

ParameterValue
```

### Url Segment

Unlike `GetOrPost`, this `ParameterType` replaces placeholder values in the `RequestUrl`:

```csharp
var request = new RestRequest("health/{entity}/status")
    .AddUrlSegment("entity", "s2");
```

When the request executes, RestSharp will try to match any `{placeholder}` with a parameter of that name (without the `{}`) and replace it with the value. So the above code results in `health/s2/status` being the url.

### Request Body

If this parameter is set, its value will be sent as the body of the request. *Only one* `RequestBody` parameter is accepted - the first one.

The name of the parameter will be used as the `Content-Type` header for the request.

`RequestBody` does not work on `GET` or `HEAD` Requests, as they do not send a body.

If you have `GetOrPost` parameters as well, they will overwrite the `RequestBody` - RestSharp will not combine them, but it will instead throw the `RequestBody` parameter away.

We recommend using `AddJsonBody` or `AddXmlBody` methods instead of `AddParameter` with type `BodyParameter`. Those methods will set the proper request type and do the serialization work for you.

#### AddJsonBody

When you call `AddJsonBody`, it does the following for you:

- Instructs the RestClient to serialize the object parameter as JSON when making a request
- Sets the content type to `application/json`
- Sets the internal data type of the request body to `DataType.Json`

Do not set content type headers or send JSON string or some sort of `JObject` instance to `AddJsonBody`, it won't work!

Here is the example:

```csharp
var param = new MyClass { IntData = 1, StringData = "test123" };
request.AddJsonBody(param);
```

#### AddXmlBody

When you call `AddXmlBody`, it does the following for you:

- Instructs the RestClient to serialize the object parameter as XML when making a request
- Sets the content type to `application/xml`
- Sets the internal data type of the request body to `DataType.Xml`

Do not set content type headers or send XML string to `AddXmlBody`, it won't work!

### Query String

This works like `GetOrPost`, except that it always appends the parameters to the url in the form `url?name1=value1&name2=value2`, regardless of the request method.

Example:

```csharp
var client = new RestClient("https://search.me");
var request = new RestRequest("search")
    .AddParameter("foo", "bar");
var response = await client.GetAsync<SearchResponse>(request);
```

It will send a `GET` request to `https://search.me/search?foo=bar")`.

You can also specify the query string parameter type explicitly:

```csharp
request.AddParameter("foo", "bar", ParameterType.QueryString);
```

In some cases you might need to prevent RestSharp from encoding the query string parameter. To do so, use the `QueryStringWithoutEncode` parameter type.

## Serialization

RestSharp has JSON and XML serializers built in. 

:::tip
The default behavior of RestSharp is to swallow deserialization errors and return `null` in the `Data`
property of the response. Read more about it in the [Error Handling](#error-handling).
:::

### JSON

The default JSON serializer uses `System.Text.Json`, which is a part of .NET since .NET 6. For earlier versions, it is added as a dependency. There are also a few serializers provided as additional packages.

### XML

The default XML serializer is `DotNetXmlSerializer`, which uses `System.Xml.Serialization` library from .NET.

### NewtonsoftJson (aka Json.Net)

The `NewtonsoftJson` package is the most popular JSON serializer for .NET. It handles all possible scenarios and is very configurable. Such a flexibility comes with the cost of performance. If you need speed, keep the default JSON serializer.

RestSharp support Json.Net serializer via a separate package. You can install it
from NuGet:

```
dotnet add package RestSharp.Serializers.NewtonsoftJson
```

Use the extension method provided by the package to configure the client:

```csharp
client.UseNewtonsoftJson();
```

The serializer configures some options by default:

```csharp
JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
{
    ContractResolver     = new CamelCasePropertyNamesContractResolver(),
    DefaultValueHandling = DefaultValueHandling.Include,
    TypeNameHandling     = TypeNameHandling.None,
    NullValueHandling    = NullValueHandling.Ignore,
    Formatting           = Formatting.None,
    ConstructorHandling  = ConstructorHandling.AllowNonPublicDefaultConstructor
};
```

If you need to use different settings, you can supply your instance of
`JsonSerializerSettings` as a parameter for the extension method.

### Custom

You can also implement your custom serializer. To support both serialization and
deserialization, you must implement the `IRestSerializer` interface.

Here is an example of a custom serializer that uses `System.Text.Json`:

```csharp
public class SimpleJsonSerializer : IRestSerializer
{
    public string Serialize(object obj) => JsonSerializer.Serialize(obj);

    public string Serialize(Parameter bodyParameter) => Serialize(bodyParameter.Value);

    public T Deserialize<T>(IRestResponse response) => JsonSerializer.Deserialize<T>(response.Content);

    public string[] SupportedContentTypes { get; } =
    {
        "application/json", "text/json", "text/x-json", "text/javascript", "*+json"
    };

    public string ContentType { get; set; } = "application/json";

    public DataFormat DataFormat { get; } = DataFormat.Json;
}
```

The value of the `SupportedContentTypes` property will be used to match the
serializer with the response `Content-Type` headers.

The `ContentType` property will be used when making a request so the
server knows how to handle the payload.

## Error handling

If there is a network transport error (network is down, failed DNS lookup, etc), or any kind of server error (except 404), `RestResponse.ResponseStatus` will be set to `ResponseStatus.Error`, otherwise it will be `ResponseStatus.Completed`.

If an API returns a 404, `ResponseStatus` will still be `Completed`. If you need access to the HTTP status code returned you will find it at `RestResponse.StatusCode`.
The `Status` property is an indicator of completion independent of the API error handling.

Normally, RestSharp doesn't throw an exception if the request fails.

However, it is possible to configure RestSharp to throw in different situations, when it normally doesn't throw
in favour of giving you the error as a property.

| Property                      | Behavior                                                                                                                                                                                                                                                                                         |
|-------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `FailOnDeserializationError`  | Changes the default behavior when failed deserialization results in a successful response with an empty `Data` property of the response. Setting this property to `true` will tell RestSharp to consider failed deserialization as an error and set the `ResponseStatus` to `Error` accordingly. |
| `ThrowOnDeserializationError` | Changes the default behavior when failed deserialization results in empty `Data` property of the response. Setting this property to `true` will tell RestSharp to throw when deserialization fails.                                                                                              |
| `ThrowOnAnyError`             | Setting this property to `true` changes the default behavior and forces RestSharp to throw if any errors occurs when making a request or during deserialization.                                                                                                                                 |

Those properties are available for the `RestClient` instance and will be used for all request made with that instance.

There are also slight differences on how different overloads handle exceptions.

Asynchronous generic methods `GetAsync<T>`, `PostAsync<T>` and so on, which aren't a part of `RestClient` interface (those methods are extension methods) return `Task<T>`. It means that there's no `RestResponse` to set the response status to error. We decided to throw an exception when such a request fails. It is a trade-off between the API consistency and usability of the library. Usually, you only need the content of `RestResponse` instance to diagnose issues and most of the time the exception would tell you what's wrong. 
