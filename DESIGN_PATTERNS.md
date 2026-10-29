# Design Patterns in .NET (C#)

A practical reference for the 23 classic Gang of Four (GoF) patterns, plus the
patterns that are built into the .NET platform and common architectural patterns
used in .NET applications.

Each pattern below covers:

- **Intent** is the problem it solves.
- **When to use it** covers when it fits and when it does not.
- **C# example** is minimal, modern C# (nullable enabled, file-scoped namespaces).
- **In .NET** lists framework types that already implement the pattern.

> Patterns are vocabulary, not goals. Reach for one when it removes a real
> source of coupling or duplication, not because the name sounds impressive.

---

## Table of contents

**Creational**

1. [Singleton](#1-singleton)
2. [Factory Method](#2-factory-method)
3. [Abstract Factory](#3-abstract-factory)
4. [Builder](#4-builder)
5. [Prototype](#5-prototype)

**Structural**

6. [Adapter](#6-adapter)
7. [Bridge](#7-bridge)
8. [Composite](#8-composite)
9. [Decorator](#9-decorator)
10. [Facade](#10-facade)
11. [Flyweight](#11-flyweight)
12. [Proxy](#12-proxy)

**Behavioral**

13. [Chain of Responsibility](#13-chain-of-responsibility)
14. [Command](#14-command)
15. [Interpreter](#15-interpreter)
16. [Iterator](#16-iterator)
17. [Mediator](#17-mediator)
18. [Memento](#18-memento)
19. [Observer](#19-observer)
20. [State](#20-state)
21. [Strategy](#21-strategy)
22. [Template Method](#22-template-method)
23. [Visitor](#23-visitor)

**.NET platform patterns**

- [Patterns already in the framework](#patterns-already-in-the-framework)
- [Architectural patterns used in .NET apps](#architectural-patterns-used-in-net-apps)

**Reference**

- [Quick reference table](#quick-reference-table)
- [Choosing a pattern](#choosing-a-pattern)
- [Common pitfalls](#common-pitfalls)
- [Further reading](#further-reading)

---

## Creational patterns

Creational patterns deal with object creation: *who* creates objects, *when*,
and *how* they are wired together.

---

### 1. Singleton

**Intent:** Guarantee a single instance of a type and provide a global access
point to it.

**When to use it**

- Exactly one instance must coordinate shared state (a cache owner, a hardware
  device wrapper, a process-wide registry).
- Prefer dependency injection with a singleton *lifetime* over a hand-written
  `Instance` property. Manual singletons make testing hard.

**C# example (thread-safe, lazy)**

```csharp
namespace Patterns.Creational;

public sealed class AppConfiguration
{
    private static readonly Lazy<AppConfiguration> LazyInstance =
        new(() => new AppConfiguration(), isThreadSafe: true);

    public static AppConfiguration Instance => LazyInstance.Value;

    private AppConfiguration()
    {
        EnvironmentName = "Development";
    }

    public string EnvironmentName { get; set; }
}
```

Usage:

```csharp
var name = AppConfiguration.Instance.EnvironmentName;
```

**DI alternative (preferred in ASP.NET Core)**

```csharp
builder.Services.AddSingleton<IClock, SystemClock>();
```

**In .NET**

- DI container singleton lifetime (`AddSingleton`).
- Process-wide static APIs such as `Console` and `Environment` provide global access, but they are not Singleton objects.

**Watch out:** a singleton is global mutable state. If it grows more than a
handful of members, split it or inject it.

---

### 2. Factory Method

**Intent:** Define an interface for creating an object, but let subclasses decide
which concrete type to instantiate.

**When to use it**

- The caller knows *what* it needs but not *which concrete type* to construct.
- Object construction is non-trivial (validation, defaults, dependency wiring).
- You want to keep `new` out of business logic so it can be tested.

**C# example**

```csharp
namespace Patterns.Creational;

public interface INotification
{
    void Send(string message);
}

public sealed class EmailNotification : INotification
{
    public void Send(string message) => Console.WriteLine($"Email: {message}");
}

public sealed class SmsNotification : INotification
{
    public void Send(string message) => Console.WriteLine($"SMS: {message}");
}

public enum Channel
{
    Email,
    Sms
}

public static class NotificationFactory
{
    public static INotification Create(Channel channel) => channel switch
    {
        Channel.Email => new EmailNotification(),
        Channel.Sms => new SmsNotification(),
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
    };
}
```

Usage:

```csharp
INotification notification = NotificationFactory.Create(Channel.Sms);
notification.Send("Build succeeded");
```

The static factory above is a common C# variant. In the GoF Factory Method, an
abstract creator declares the creation step and a subclass supplies the product.

```csharp
namespace Patterns.Creational;

public abstract class NotificationService
{
    public void Notify(string message) => CreateNotification().Send(message);

    protected abstract INotification CreateNotification();
}

public sealed class SmsNotificationService : NotificationService
{
    protected override INotification CreateNotification() => new SmsNotification();
}
```

**In .NET**

- `ILoggerFactory.CreateLogger<T>()`, `DbProviderFactory.CreateConnection()`.
- `Activator.CreateInstance`, `IHttpClientFactory`, `WebApplication.CreateBuilder`.
- `Func<T>` registrations in DI: `services.AddTransient<Func<Channel, INotification>>(...)`.

**Watch out:** a `switch` factory is fine until it becomes a 30-branch monster.
Then prefer a registry or the concrete factory registration in DI.

---

### 3. Abstract Factory

**Intent:** Create families of related objects without specifying their concrete
classes. The client works with one factory and gets a consistent set of products.

**When to use it**

- Products must match (Windows controls with Windows look, SQL Server types with
  SQL Server behavior, one cloud provider's storage, queue, and secret store).
- You want to swap an entire family at configuration time.

**C# example**

```csharp
namespace Patterns.Creational;

public interface IButton
{
    void Render();
}

public interface ICheckbox
{
    void Render();
}

public interface IUiFactory
{
    IButton CreateButton();
    ICheckbox CreateCheckbox();
}

public sealed class WindowsUiFactory : IUiFactory
{
    public IButton CreateButton() => new WindowsButton();
    public ICheckbox CreateCheckbox() => new WindowsCheckbox();
}

public sealed class MacUiFactory : IUiFactory
{
    public IButton CreateButton() => new MacButton();
    public ICheckbox CreateCheckbox() => new MacCheckbox();
}

internal sealed class WindowsButton : IButton
{
    public void Render() => Console.WriteLine("[ Windows button ]");
}

internal sealed class WindowsCheckbox : ICheckbox
{
    public void Render() => Console.WriteLine("[x] Windows checkbox");
}

internal sealed class MacButton : IButton
{
    public void Render() => Console.WriteLine("( Mac button )");
}

internal sealed class MacCheckbox : ICheckbox
{
    public void Render() => Console.WriteLine("(x) Mac checkbox");
}
```

Client code stays independent of the concrete family:

```csharp
static void RenderForm(IUiFactory factory)
{
    factory.CreateButton().Render();
    factory.CreateCheckbox().Render();
}
```

**In .NET**

- `DbProviderFactory` (connection + command + parameter families).
- `ILoggerProvider`, `IServiceProvider`-driven provider families.
- Cloud SDK abstractions (AWS, Azure, or GCP) implemented behind one interface family.

**Factory Method vs Abstract Factory:** Factory Method creates *one* product.
Abstract Factory creates a *family*. Abstract Factory is often implemented with
several Factory Methods.

---

### 4. Builder

**Intent:** Construct a complex object step by step. The same construction process
can produce different representations.

**When to use it**

- Constructors have many parameters, especially optional or boolean ones.
- Object creation requires validation between steps.
- You want an immutable result assembled from a fluent API.

**C# example (fluent builder with validation)**

```csharp
namespace Patterns.Creational;

public sealed record HttpRequest(
    string Method,
    string Url,
    IReadOnlyDictionary<string, string> Headers,
    string? Body);

public sealed class HttpRequestBuilder
{
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);
    private string _method = "GET";
    private string? _url;
    private string? _body;

    public HttpRequestBuilder WithMethod(string method)
    {
        _method = method;
        return this;
    }

    public HttpRequestBuilder WithUrl(string url)
    {
        _url = url;
        return this;
    }

    public HttpRequestBuilder WithHeader(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    public HttpRequestBuilder WithBody(string body)
    {
        _body = body;
        return this;
    }

    public HttpRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_url))
        {
            throw new InvalidOperationException("URL is required.");
        }

        return new HttpRequest(
            _method,
            _url,
            new Dictionary<string, string>(_headers, _headers.Comparer),
            _body);
    }
}
```

Usage:

```csharp
var request = new HttpRequestBuilder()
    .WithMethod("POST")
    .WithUrl("https://api.example.com/orders")
    .WithHeader("Authorization", "Bearer token")
    .WithBody("""{"total":42}""")
    .Build();
```

**In .NET**

- `StringBuilder` (incremental construction).
- `HostBuilder` and `WebApplicationBuilder` (fluent configuration).
- `UriBuilder`, `SqlConnectionStringBuilder`, `ConfigurationBuilder`.
- `record` `with` expressions for copy-and-modify (adjacent to Builder).

**Watch out:** builders that only set properties duplicate object initializers.
Justify the builder by validation, ordering, or reuse of a construction recipe.

---

### 5. Prototype

**Intent:** Create new objects by cloning an existing instance instead of
constructing from scratch.

**When to use it**

- Construction is expensive (large object graphs, configuration loading).
- You need many near-identical objects with small variations.
- You want to preserve state without exposing constructors.

**C# example (records and explicit deep copy)**

```csharp
namespace Patterns.Creational;

public sealed record InvoiceLine(string Description, decimal Amount);

public sealed record Invoice(
    string Customer,
    DateOnly IssuedOn,
    IReadOnlyList<InvoiceLine> Lines)
{
    public Invoice WithDiscount(decimal percentage)
    {
        var discounted = Lines
            .Select(line => line with { Amount = line.Amount * (1 - percentage / 100m) })
            .ToArray();

        return this with { Lines = discounted };
    }
}
```

Usage:

```csharp
var template = new Invoice(
    "Contoso",
    DateOnly.FromDateTime(DateTime.UtcNow),
    new[] { new InvoiceLine("Support", 500m), new InvoiceLine("Hosting", 120m) });

var discounted = template.WithDiscount(10m);
```

For mutable classes, implement a dedicated `Clone()` method and copy collections
explicitly. `MemberwiseClone` is shallow, and `ICloneable` is rarely the right
public API.

**In .NET**

- `record` and `with` expressions copy an object and apply changes.
- `Array.Clone` and `List<T>` copy constructors make shallow copies.
- `ICloneable` exists, but it does not say whether the copy is shallow or deep.

---

## Structural patterns

Structural patterns describe how types and objects are composed into larger
structures while keeping them flexible.

---

### 6. Adapter

**Intent:** Convert one interface into another that clients expect. It lets
otherwise incompatible types work together.

**When to use it**

- Integrating a third-party or legacy API behind your own abstraction.
- Normalizing several providers behind one internal interface.
- Isolating vendor types at the boundary of your application.

**C# example**

```csharp
namespace Patterns.Structural;

public sealed class LegacyXmlLogger
{
    public void WriteXmlEntry(string level, string text) =>
        Console.WriteLine($"<log level=\"{level}\">{text}</log>");
}

public interface IAppLogger
{
    void Info(string message);
    void Error(string message);
}

public sealed class LegacyLoggerAdapter : IAppLogger
{
    private readonly LegacyXmlLogger _legacy;

    public LegacyLoggerAdapter(LegacyXmlLogger legacy) => _legacy = legacy;

    public void Info(string message) => _legacy.WriteXmlEntry("info", message);

    public void Error(string message) => _legacy.WriteXmlEntry("error", message);
}
```

**In .NET**

- `StreamReader` and `StreamWriter` adapt a byte stream to text.
- `NetworkStream` adapts a socket to a `Stream`.
- `IEnumerable<T>` adapters such as `Enumerable.Cast<T>()`.
- gRPC or HTTP client wrappers around a domain interface.

---

### 7. Bridge

**Intent:** Separate an abstraction from its implementation so both can vary
independently. Instead of a class explosion from combining dimensions, you
compose two hierarchies.

**When to use it**

- You have two dimensions of variation (shape × renderer, message type × channel).
- You want to swap implementations at runtime.
- Inheritance would produce `M×N` subclasses.

**C# example**

```csharp
namespace Patterns.Structural;

public interface IMessageChannel
{
    void Deliver(string title, string body);
}

public sealed class EmailChannel : IMessageChannel
{
    public void Deliver(string title, string body) =>
        Console.WriteLine($"Email [{title}] {body}");
}

public sealed class SlackChannel : IMessageChannel
{
    public void Deliver(string title, string body) =>
        Console.WriteLine($"Slack [{title}] {body}");
}

public abstract class Message
{
    protected Message(IMessageChannel channel) => Channel = channel;

    protected IMessageChannel Channel { get; }

    public abstract void Send();
}

public sealed class AlertMessage : Message
{
    private readonly string _text;

    public AlertMessage(IMessageChannel channel, string text) : base(channel) =>
        _text = text;

    public override void Send() => Channel.Deliver("ALERT", _text);
}

public sealed class DigestMessage : Message
{
    private readonly IReadOnlyList<string> _items;

    public DigestMessage(IMessageChannel channel, IReadOnlyList<string> items) : base(channel) =>
        _items = items;

    public override void Send() => Channel.Deliver("DIGEST", string.Join("; ", _items));
}
```

Usage:

```csharp
Message message = new AlertMessage(new SlackChannel(), "Disk is 95% full");
message.Send();
```

**In .NET**

- `ILogger` providers (logging abstraction + provider implementations).
- ADO.NET: `DbConnection` abstraction + provider-specific implementations.
- .NET MAUI handlers (cross-platform control abstraction plus platform-specific handlers).

**Bridge vs Strategy:** the structures look similar. Use Bridge when both the
abstraction and the implementation are expected to grow their own hierarchies.
Use Strategy when you are swapping one algorithm behind one interface.

---

### 8. Composite

**Intent:** Compose objects into tree structures and treat individual objects and
groups uniformly.

**When to use it**

- Hierarchical data: file systems, UI trees, organization charts, menu structures.
- Clients should not care whether they hold a leaf or a container.
- You need recursive operations (sum, render, validate) over a tree.

**C# example**

```csharp
namespace Patterns.Structural;

public abstract class FileSystemNode
{
    protected FileSystemNode(string name) => Name = name;

    public string Name { get; }

    public abstract long GetSizeInBytes();

    public virtual void Print(string indent = "") =>
        Console.WriteLine($"{indent}{Name} ({GetSizeInBytes()} bytes)");
}

public sealed class FileNode : FileSystemNode
{
    private readonly long _size;

    public FileNode(string name, long size) : base(name) => _size = size;

    public override long GetSizeInBytes() => _size;
}

public sealed class DirectoryNode : FileSystemNode
{
    private readonly List<FileSystemNode> _children = new();

    public DirectoryNode(string name) : base(name) { }

    public void Add(FileSystemNode node) => _children.Add(node);

    public override long GetSizeInBytes() => _children.Sum(child => child.GetSizeInBytes());

    public override void Print(string indent = "")
    {
        base.Print(indent);
        foreach (var child in _children)
        {
            child.Print(indent + "  ");
        }
    }
}
```

Usage:

```csharp
var root = new DirectoryNode("src");
var patterns = new DirectoryNode("patterns");
patterns.Add(new FileNode("Singleton.cs", 1024));
root.Add(patterns);
root.Add(new FileNode("Program.cs", 512));
root.Print();
```

**In .NET**

- `System.Linq.Expressions.Expression` trees.
- `XElement` and `XNode` in LINQ to XML.
- UI control trees (WPF `Visual`, WinForms `Control`).
- `DirectoryInfo` and `FileSystemInfo` hierarchies (loosely).

**Watch out:** a uniform interface can force leaf types to implement methods that
make no sense (`Add` on a file). Decide whether to throw or to split interfaces.

---

### 9. Decorator

**Intent:** Attach additional responsibilities to an object dynamically by
wrapping it in another object with the same interface.

**When to use it**

- Cross-cutting behavior: caching, logging, retries, metrics, validation.
- You want to combine behaviors in any order at runtime.
- Inheritance would create a subclass for every combination.

**C# example**

```csharp
namespace Patterns.Structural;

public interface IPriceProvider
{
    decimal GetPrice(string sku);
}

public sealed class DatabasePriceProvider : IPriceProvider
{
    public decimal GetPrice(string sku) => sku switch
    {
        "BOOK" => 20m,
        "PEN" => 2m,
        _ => 0m
    };
}

public sealed class CachingPriceProvider : IPriceProvider
{
    private readonly IPriceProvider _inner;
    private readonly Dictionary<string, decimal> _cache = new(StringComparer.Ordinal);

    public CachingPriceProvider(IPriceProvider inner) => _inner = inner;

    public decimal GetPrice(string sku)
    {
        if (_cache.TryGetValue(sku, out var cached))
        {
            return cached;
        }

        var price = _inner.GetPrice(sku);
        _cache[sku] = price;
        return price;
    }
}

public sealed class LoggingPriceProvider : IPriceProvider
{
    private readonly IPriceProvider _inner;

    public LoggingPriceProvider(IPriceProvider inner) => _inner = inner;

    public decimal GetPrice(string sku)
    {
        var price = _inner.GetPrice(sku);
        Console.WriteLine($"Price for {sku} = {price}");
        return price;
    }
}
```

Composition:

```csharp
IPriceProvider provider = new LoggingPriceProvider(
    new CachingPriceProvider(
        new DatabasePriceProvider()));
```

**In .NET**

- `Stream` decorators (`BufferedStream`, `GZipStream`, `CryptoStream`).
- ASP.NET Core middleware (each component wraps the next).
- `DelegatingHandler` in `HttpClient` pipelines.
- DI decorators registered manually or with a decorator library.

**Decorator vs Proxy:** both wrap. A decorator *adds* behavior. A proxy *controls
access* to the wrapped object.

**Decorator vs Adapter:** an adapter changes the interface. A decorator keeps it.

---

### 10. Facade

**Intent:** Provide a simplified, unified interface to a set of interfaces in a
subsystem.

**When to use it**

- A subsystem is complex and clients only need a few high-level operations.
- You want to decouple application code from many vendor types.
- You are defining a bounded-context boundary.

**C# example**

```csharp
namespace Patterns.Structural;

public sealed class VideoDecoder
{
    public void Decode(string file) => Console.WriteLine($"Decoding {file}");
}

public sealed class AudioDecoder
{
    public void Decode(string file) => Console.WriteLine($"Decoding audio {file}");
}

public sealed class SubtitleLoader
{
    public void Load(string file) => Console.WriteLine($"Loading subtitles {file}");
}

public sealed class VideoPlayer
{
    private readonly VideoDecoder _video = new();
    private readonly AudioDecoder _audio = new();
    private readonly SubtitleLoader _subtitles = new();

    public void Play(string file)
    {
        _video.Decode(file);
        _audio.Decode(file);
        _subtitles.Load(file);
        Console.WriteLine($"Playing {file}");
    }
}
```

Usage:

```csharp
new VideoPlayer().Play("movie.mkv");
```

**In .NET**

- `HttpClient` (a facade over sockets, headers, redirects, and connection pooling).
- `File` static class over `FileStream` internals.
- `EmailSender` or `IEmailSender` wrappers over SMTP.
- Application services in clean architecture.

**Watch out:** a facade can become a god class. Keep it to orchestration, not
business rules.

---

### 11. Flyweight

**Intent:** Share common state between many objects to reduce memory use.
Intrinsic (shared, immutable) state is stored once. Extrinsic state is passed in.

**When to use it**

- Very large numbers of objects with lots of shared data (glyphs, tiles, particles).
- Memory pressure is measurable and shared state is immutable.
- You can key objects by a small set of values.

**C# example**

```csharp
namespace Patterns.Structural;

public sealed class Glyph
{
    public Glyph(char symbol, string font, int size)
    {
        Symbol = symbol;
        Font = font;
        Size = size;
    }

    public char Symbol { get; }
    public string Font { get; }
    public int Size { get; }

    public void Draw(int x, int y) =>
        Console.WriteLine($"'{Symbol}' {Font} {Size}px at ({x},{y})");
}

public sealed class GlyphFactory
{
    private readonly Dictionary<(char Symbol, string Font, int Size), Glyph> _cache = new();

    public Glyph Get(char symbol, string font, int size)
    {
        var key = (symbol, font, size);
        if (!_cache.TryGetValue(key, out var glyph))
        {
            glyph = new Glyph(symbol, font, size);
            _cache[key] = glyph;
        }

        return glyph;
    }
}

public sealed record CharacterRun(Glyph Glyph, int X, int Y);
```

Usage:

```csharp
var factory = new GlyphFactory();
var runs = "hello".Select((c, i) => new CharacterRun(factory.Get(c, "Consolas", 12), i * 10, 0));
```

**In .NET**

- `string.Intern` and the CLR string pool.
- `Encoding` and `CultureInfo` caches.
- `ArrayPool<T>` and `MemoryPool<T>` (object pooling rather than sharing, adjacent idea).
- `Type` objects and metadata tokens.

**Watch out:** only optimize when memory is actually a problem. Measure first.

---

### 12. Proxy

**Intent:** Provide a surrogate or placeholder for another object to control
access to it.

**When to use it**

- Lazy loading (create the real object on first use).
- Access control (check permissions before delegating).
- Remote access (local representative of a remote service).
- Caching or reference counting around an expensive object.

**C# example (lazy, access-controlled proxy)**

```csharp
namespace Patterns.Structural;

public interface IDocument
{
    string Read();
}

public sealed class DiskDocument : IDocument
{
    private readonly string _path;

    public DiskDocument(string path) => _path = path;

    public string Read() => File.ReadAllText(_path);
}

public sealed class ProtectedDocumentProxy : IDocument
{
    private readonly string _path;
    private readonly Func<bool> _canRead;
    private DiskDocument? _real;

    public ProtectedDocumentProxy(string path, Func<bool> canRead)
    {
        _path = path;
        _canRead = canRead;
    }

    public string Read()
    {
        if (!_canRead())
        {
            throw new UnauthorizedAccessException("Read access denied.");
        }

        _real ??= new DiskDocument(_path);
        return _real.Read();
    }
}
```

**In .NET**

- `DispatchProxy` (runtime-generated proxies used by mocking frameworks).
- EF Core lazy-loading proxies (`Microsoft.EntityFrameworkCore.Proxies`).
- WCF or .NET Remoting transparent proxies (legacy).
- `Castle DynamicProxy` (third-party, used by many frameworks).

**Proxy vs Decorator:** proxy controls access and usually manages the wrapped
object's lifecycle. A decorator adds behavior around an object supplied to it.

---

## Behavioral patterns

Behavioral patterns deal with communication between objects and the assignment
of responsibilities.

---

### 13. Chain of Responsibility

**Intent:** Pass a request along a chain of handlers until one handles it. Each
handler decides whether to process or forward the request.

**When to use it**

- Multiple steps can handle a request and the handler is chosen at runtime.
- You want to add or remove processing steps without touching callers.
- Validation pipelines, approval workflows, middleware.

**C# example**

```csharp
namespace Patterns.Behavioral;

public sealed record Expense(decimal Amount, string Description);

public abstract class ExpenseHandler
{
    private ExpenseHandler? _next;

    public ExpenseHandler SetNext(ExpenseHandler next)
    {
        _next = next;
        return next;
    }

    public virtual string Handle(Expense expense) =>
        _next?.Handle(expense) ?? $"No handler for {expense.Description}";
}

public sealed class TeamLeadHandler : ExpenseHandler
{
    public override string Handle(Expense expense) =>
        expense.Amount <= 500m
            ? $"Approved by team lead: {expense.Description}"
            : base.Handle(expense);
}

public sealed class ManagerHandler : ExpenseHandler
{
    public override string Handle(Expense expense) =>
        expense.Amount <= 5_000m
            ? $"Approved by manager: {expense.Description}"
            : base.Handle(expense);
}

public sealed class DirectorHandler : ExpenseHandler
{
    public override string Handle(Expense expense) =>
        $"Approved by director: {expense.Description}";
}
```

Composition:

```csharp
var chain = new TeamLeadHandler();
chain.SetNext(new ManagerHandler()).SetNext(new DirectorHandler());

Console.WriteLine(chain.Handle(new Expense(12_000m, "Conference trip")));
```

**In .NET**

- ASP.NET Core middleware pipeline.
- `DelegatingHandler` chains in `HttpClient`.
- Exception handling: `catch` blocks searched in order.

**Watch out:** chains can be hard to debug. Log transitions or keep the chain
short and explicit.

---

### 14. Command

**Intent:** Encapsulate a request as an object, allowing parameterization,
queuing, logging, and undo.

**When to use it**

- You need undo and redo.
- Requests must be queued, scheduled, or retried.
- You want to log or audit operations.
- UI actions should be decoupled from business logic.

**C# example (with undo)**

```csharp
namespace Patterns.Behavioral;

public interface ICommand
{
    void Execute();
    void Undo();
}

public sealed class TextDocument
{
    public string Text { get; set; } = string.Empty;
}

public sealed class InsertTextCommand : ICommand
{
    private readonly TextDocument _document;
    private readonly string _text;
    private readonly Stack<string> _previous = new();

    public InsertTextCommand(TextDocument document, string text)
    {
        _document = document;
        _text = text;
    }

    public void Execute()
    {
        _previous.Push(_document.Text);
        _document.Text += _text;
    }

    public void Undo()
    {
        if (_previous.TryPop(out var previous))
        {
            _document.Text = previous;
        }
    }
}

public sealed class CommandHistory
{
    private readonly Stack<ICommand> _undoStack = new();

    public void Execute(ICommand command)
    {
        command.Execute();
        _undoStack.Push(command);
    }

    public void Undo()
    {
        if (_undoStack.TryPop(out var command))
        {
            command.Undo();
        }
    }
}
```

Usage:

```csharp
var document = new TextDocument();
var history = new CommandHistory();
history.Execute(new InsertTextCommand(document, "Hello"));
history.Execute(new InsertTextCommand(document, " world"));
history.Undo();
Console.WriteLine(document.Text); // Hello
```

**In .NET**

- `System.Windows.Input.ICommand` (WPF and MVVM).
- `System.CommandLine.Command` with `SetAction` (CLI commands).
- Message and queue handlers (MediatR `IRequestHandler` is a command-dispatcher variant).

**Watch out:** command objects that wrap a single method call with no undo,
queueing, or logging need are extra allocations. Call the method.

---

### 15. Interpreter

**Intent:** Given a language, define a representation for its grammar along with
an interpreter that evaluates sentences in the language.

**When to use it**

- You need to evaluate a small, well-defined DSL (rules, filters, expressions).
- The grammar is simple and changes slowly.
- You do *not* need parser generators, performance, or complex syntax.

**C# example (boolean rule DSL)**

```csharp
namespace Patterns.Behavioral;

public interface IRuleExpression
{
    bool Evaluate(IReadOnlyDictionary<string, int> context);
}

public sealed class GreaterThanRule : IRuleExpression
{
    private readonly string _variable;
    private readonly int _threshold;

    public GreaterThanRule(string variable, int threshold)
    {
        _variable = variable;
        _threshold = threshold;
    }

    public bool Evaluate(IReadOnlyDictionary<string, int> context) =>
        context.TryGetValue(_variable, out var value) && value > _threshold;
}

public sealed class AndRule : IRuleExpression
{
    private readonly IRuleExpression _left;
    private readonly IRuleExpression _right;

    public AndRule(IRuleExpression left, IRuleExpression right)
    {
        _left = left;
        _right = right;
    }

    public bool Evaluate(IReadOnlyDictionary<string, int> context) =>
        _left.Evaluate(context) && _right.Evaluate(context);
}

public sealed class OrRule : IRuleExpression
{
    private readonly IRuleExpression _left;
    private readonly IRuleExpression _right;

    public OrRule(IRuleExpression left, IRuleExpression right)
    {
        _left = left;
        _right = right;
    }

    public bool Evaluate(IReadOnlyDictionary<string, int> context) =>
        _left.Evaluate(context) || _right.Evaluate(context);
}
```

Usage:

```csharp
var rule = new OrRule(
    new GreaterThanRule("age", 65),
    new GreaterThanRule("yearsOfService", 30));

var eligible = rule.Evaluate(new Dictionary<string, int>
{
    ["age"] = 40,
    ["yearsOfService"] = 32
});
```

**In .NET**

- `System.Linq.Expressions` builds expression trees. `LambdaExpression.Compile()` turns a lambda into a delegate.
- `Regex` (a full regex engine, not the GoF structure).
- FluentValidation rule trees.

**Watch out:** hand-rolled interpreters do not scale to real languages. Use a
parser generator (ANTLR, Sprache) or an expression library when grammar grows.

---

### 16. Iterator

**Intent:** Access the elements of an aggregate sequentially without exposing its
underlying representation.

**When to use it**

- Custom collections that should work with `foreach` and LINQ.
- Traversal logic that should live with the collection.
- You need multiple independent traversals.
- You need lazy or streaming traversal.

**C# example**

```csharp
namespace Patterns.Behavioral;

public sealed class RingBuffer<T> : IEnumerable<T>
{
    private readonly T[] _items;
    private readonly int _count;
    private readonly int _start;

    public RingBuffer(IEnumerable<T> items, int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        }

        _items = new T[capacity];
        var count = 0;
        var next = 0;

        foreach (var item in items)
        {
            _items[next] = item;
            next = (next + 1) % capacity;
            count++;
        }

        _count = Math.Min(count, capacity);
        _start = count > capacity ? next : 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < _count; i++)
        {
            yield return _items[(_start + i) % _items.Length];
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
```

Usage:

```csharp
var buffer = new RingBuffer<int>(Enumerable.Range(1, 7), 5);
Console.WriteLine(string.Join(" ", buffer)); // 3 4 5 6 7
```

**In .NET**

- `IEnumerable<T>` and `IEnumerator<T>` with `yield return`.
- `IAsyncEnumerable<T>` and `await foreach`.
- `List<T>.Enumerator` (struct enumerator to avoid allocations).
- LINQ operators (`Where`, `Select`) are iterator pipelines.

**Watch out:** never mutate the collection while you enumerate it unless the type
supports that. A `yield` method captures state and defers execution, which can
surprise you when side effects are involved.

---

### 17. Mediator

**Intent:** Reduce chaotic dependencies between objects by making them communicate
through a central mediator instead of referring to each other directly.

**When to use it**

- Many-to-many communication between components (chat room, UI dialogs).
- You want to decouple colleagues but keep a central coordination point.
- Request and response dispatch in application layers (MediatR-style).

**C# example (chat room)**

```csharp
namespace Patterns.Behavioral;

public interface IChatRoom
{
    void Register(Participant participant);
    void Send(string from, string message);
}

public sealed class ChatRoom : IChatRoom
{
    private readonly List<Participant> _participants = new();

    public void Register(Participant participant) => _participants.Add(participant);

    public void Send(string from, string message)
    {
        foreach (var participant in _participants)
        {
            if (!string.Equals(participant.Name, from, StringComparison.Ordinal))
            {
                participant.Receive(from, message);
            }
        }
    }
}

public sealed class Participant
{
    private readonly IChatRoom _room;

    public Participant(string name, IChatRoom room)
    {
        Name = name;
        _room = room;
        room.Register(this);
    }

    public string Name { get; }

    public void Say(string message) => _room.Send(Name, message);

    public void Receive(string from, string message) =>
        Console.WriteLine($"{Name} received from {from}: {message}");
}
```

Usage:

```csharp
var room = new ChatRoom();
var alice = new Participant("Alice", room);
var bob = new Participant("Bob", room);
alice.Say("Hello");
bob.Say("Hi Alice");
```

**In .NET**

- `MediatR` (`IMediator`, `INotification`).
- UI frameworks: a dialog mediates between its controls.
- `IServiceProvider` as a lookup mediator (use sparingly, because it turns into a service locator).
- SignalR hubs mediating client messages.

**Watch out:** the mediator can become a god object. Keep handlers small and
split by feature. Do not use MediatR because it is popular. Direct calls
are often clearer.

---

### 18. Memento

**Intent:** Capture and restore an object's internal state without violating
encapsulation.

**When to use it**

- You need undo and redo, snapshots, or checkpoints.
- You must externalize state but keep it immutable and opaque to the caretaker.

**C# example**

```csharp
namespace Patterns.Behavioral;

public interface IEditorSnapshot
{
}

public sealed class TextEditor
{
    public string Text { get; private set; } = string.Empty;
    public int CursorPosition { get; private set; }

    public void Type(string text)
    {
        Text += text;
        CursorPosition = Text.Length;
    }

    public IEditorSnapshot Save() => new Snapshot(Text, CursorPosition);

    public void Restore(IEditorSnapshot snapshot)
    {
        if (snapshot is not Snapshot state)
        {
            throw new ArgumentException("Unknown snapshot type.", nameof(snapshot));
        }

        Text = state.Text;
        CursorPosition = state.CursorPosition;
    }

    private sealed record Snapshot(string Text, int CursorPosition) : IEditorSnapshot;
}

public sealed class EditorHistory
{
    private readonly Stack<IEditorSnapshot> _snapshots = new();

    public void Push(IEditorSnapshot snapshot) => _snapshots.Push(snapshot);

    public IEditorSnapshot? Pop() => _snapshots.TryPop(out var snapshot) ? snapshot : null;
}
```

Usage:

```csharp
var editor = new TextEditor();
var history = new EditorHistory();

history.Push(editor.Save());
editor.Type("Hello");
history.Push(editor.Save());
editor.Type(", world");

if (history.Pop() is { } snapshot)
{
    editor.Restore(snapshot);
}

Console.WriteLine(editor.Text); // Hello
```

**In .NET**

- `record` as an immutable snapshot.
- Undo stacks in editors and design tools.
- `System.Text.Json` serialization for persisted snapshots.
- EF Core `ChangeTracker` (state capture, not a strict memento).

**Watch out:** mementos of large graphs are expensive. Prefer copy-on-write or
command-based undo when snapshots are too heavy.

---

### 19. Observer

**Intent:** Define a one-to-many dependency so that when one object changes state,
all dependents are notified automatically.

**When to use it**

- Event handling and UI updates.
- Publishing domain events to multiple subscribers.
- Decoupling producers from consumers.

**C# example (custom subject)**

```csharp
namespace Patterns.Behavioral;

public interface ISubscriber<in T>
{
    void Update(T value);
}

public sealed class Subject<T>
{
    private readonly List<ISubscriber<T>> _observers = new();

    public IDisposable Subscribe(ISubscriber<T> observer)
    {
        _observers.Add(observer);
        return new Subscription(() => _observers.Remove(observer));
    }

    public void Publish(T value)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.Update(value);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private Action? _unsubscribe;

        public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;

        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}

public sealed class ConsoleSubscriber<T> : ISubscriber<T>
{
    public void Update(T value) => Console.WriteLine($"Received: {value}");
}
```

Usage:

```csharp
var subject = new Subject<int>();
using var subscription = subject.Subscribe(new ConsoleSubscriber<int>());
subject.Publish(42);
```

**C# events alternative**

```csharp
public sealed class TemperatureSensor
{
    public event EventHandler<double>? TemperatureChanged;

    public void SetTemperature(double value) =>
        TemperatureChanged?.Invoke(this, value);
}
```

**In .NET**

- `event` and `EventHandler<T>` (the idiomatic C# observer).
- `IObservable<T>` and `IObserver<T>` with Rx.NET.
- `INotifyPropertyChanged` and `INotifyCollectionChanged` in UI binding.
- `IOptionsMonitor<T>.OnChange`, `IHostApplicationLifetime` callbacks.
- SignalR, message buses, domain event dispatchers.

**Watch out:** events hold strong references. Always unsubscribe or use weak
events or `IDisposable` subscriptions. An exception in one observer can stop the
rest. Decide on isolation explicitly.

---

### 20. State

**Intent:** Allow an object to alter its behavior when its internal state changes.
The object appears to change its class.

**When to use it**

- A state machine with rich per-state behavior (orders, documents, connections).
- You have many `switch` statements on a status field.
- Transitions need validation and side effects.

**C# example**

```csharp
namespace Patterns.Behavioral;

public interface IOrderState
{
    string Name { get; }
    IOrderState Pay();
    IOrderState Ship();
    IOrderState Cancel();
}

public sealed class Order
{
    public Order() => State = new PendingState();

    public IOrderState State { get; private set; }

    public void Pay() => State = State.Pay();
    public void Ship() => State = State.Ship();
    public void Cancel() => State = State.Cancel();
}

public sealed class PendingState : IOrderState
{
    public string Name => "Pending";

    public IOrderState Pay() => new PaidState();

    public IOrderState Ship() => throw new InvalidOperationException("Pay first.");

    public IOrderState Cancel() => new CancelledState();
}

public sealed class PaidState : IOrderState
{
    public string Name => "Paid";

    public IOrderState Pay() => this;

    public IOrderState Ship() => new ShippedState();

    public IOrderState Cancel() => new CancelledState();
}

public sealed class ShippedState : IOrderState
{
    public string Name => "Shipped";

    public IOrderState Pay() => this;

    public IOrderState Ship() => this;

    public IOrderState Cancel() => throw new InvalidOperationException("Already shipped.");
}

public sealed class CancelledState : IOrderState
{
    public string Name => "Cancelled";

    public IOrderState Pay() => throw new InvalidOperationException("Order is cancelled.");

    public IOrderState Ship() => throw new InvalidOperationException("Order is cancelled.");

    public IOrderState Cancel() => this;
}
```

Usage:

```csharp
var order = new Order();
order.Pay();
order.Ship();
Console.WriteLine(order.State.Name); // Shipped
```

**In .NET**

- `Stream` state, `HttpClient` connection states.
- Stateless library and custom state machines.
- Workflow engines (Camunda, Durable Functions orchestrations).

**Watch out:** for simple two-state flags, an enum plus a guard is clearer than
a class hierarchy. Use State when transitions and per-state behavior are real.

---

### 21. Strategy

**Intent:** Define a family of algorithms, encapsulate each one, and make them
interchangeable at runtime.

**When to use it**

- Multiple algorithms for one task (sorting, pricing, compression, auth).
- You want to replace `if` and `else` chains or `switch` selection over policies.
- Behavior should be configurable or injected.

**C# example**

```csharp
namespace Patterns.Behavioral;

public interface IShippingStrategy
{
    decimal Calculate(decimal weightKg, decimal distanceKm);
}

public sealed class FlatRateShipping : IShippingStrategy
{
    public decimal Calculate(decimal weightKg, decimal distanceKm) => 9.99m;
}

public sealed class WeightBasedShipping : IShippingStrategy
{
    public decimal Calculate(decimal weightKg, decimal distanceKm) => weightKg * 2.5m;
}

public sealed class DistanceBasedShipping : IShippingStrategy
{
    public decimal Calculate(decimal weightKg, decimal distanceKm) => distanceKm * 0.15m;
}

public sealed class ShippingCalculator
{
    private readonly IShippingStrategy _strategy;

    public ShippingCalculator(IShippingStrategy strategy) => _strategy = strategy;

    public decimal Calculate(decimal weightKg, decimal distanceKm) =>
        _strategy.Calculate(weightKg, distanceKm);
}
```

Usage:

```csharp
var calculator = new ShippingCalculator(new WeightBasedShipping());
var cost = calculator.Calculate(weightKg: 3m, distanceKm: 400m);
Console.WriteLine(cost); // 7.5
```

DI registration with a keyed strategy:

```csharp
builder.Services.AddKeyedSingleton<IShippingStrategy, FlatRateShipping>("flat");
builder.Services.AddKeyedSingleton<IShippingStrategy, WeightBasedShipping>("weight");
```

**In .NET**

- `IComparer<T>` and `Comparison<T>` passed to `List<T>.Sort`.
- `IEqualityComparer<T>` for dictionaries and LINQ.
- `IAuthenticationHandler`, `IOutputFormatter`, `ICompressionProvider`.
- `Func<T, TResult>` delegates as lightweight strategies.

**Strategy vs State:** Strategy selects an algorithm. State transitions itself
as a consequence of operations. Strategy is usually stateless and injected.
State is stateful and drives transitions.

---

### 22. Template Method

**Intent:** Define the skeleton of an algorithm in a base class, deferring some
steps to subclasses without changing the algorithm's structure.

**When to use it**

- Several classes share a workflow but differ in specific steps.
- The order of steps is fixed and must not be overridden.
- You want hooks around a fixed process.

**C# example**

```csharp
namespace Patterns.Behavioral;

public abstract class DataExporter
{
    public void Export(string destination)
    {
        var data = Load();
        var transformed = Transform(data);
        Validate(transformed);
        Write(destination, transformed);
        OnCompleted(destination);
    }

    protected abstract IReadOnlyList<string> Load();

    protected virtual IReadOnlyList<string> Transform(IReadOnlyList<string> data) => data;

    protected virtual void Validate(IReadOnlyList<string> data) { }

    protected abstract void Write(string destination, IReadOnlyList<string> data);

    protected virtual void OnCompleted(string destination) { }
}

public sealed class CsvExporter : DataExporter
{
    protected override IReadOnlyList<string> Load() =>
        new[] { "id,name", "1,Alice", "2,Bob" };

    protected override void Write(string destination, IReadOnlyList<string> data) =>
        File.WriteAllLines(destination, data);
}

public sealed class JsonExporter : DataExporter
{
    protected override IReadOnlyList<string> Load() =>
        new[] { """{"id":1,"name":"Alice"}""", """{"id":2,"name":"Bob"}""" };

    protected override IReadOnlyList<string> Transform(IReadOnlyList<string> data) =>
        new[] { "[" + string.Join(",", data) + "]" };

    protected override void Write(string destination, IReadOnlyList<string> data) =>
        File.WriteAllLines(destination, data);
}
```

Usage:

```csharp
new CsvExporter().Export("orders.csv");
```

**In .NET**

- `Stream` and `TextWriter` base classes.
- ASP.NET Core MVC action filters (`Controller.OnActionExecutionAsync`).
- EF Core `DbContext.SaveChanges` pipeline with overridable hooks.

**C# alternative:** in modern C#, composition with delegates or Strategy is often
preferable to inheritance. Prefer Template Method when the skeleton genuinely
belongs to the base type.

---

### 23. Visitor

**Intent:** Represent an operation to be performed on elements of an object
structure. Visitor lets you add new operations without changing the element
classes.

**When to use it**

- A stable set of types needs many unrelated operations (export, validation,
  pretty-printing, metrics).
- You want double dispatch in C#.
- You control the element hierarchy but not the operation set.

**C# example (expression tree evaluation and printing)**

```csharp
namespace Patterns.Behavioral;

public interface IExpression
{
    T Accept<T>(IExpressionVisitor<T> visitor);
}

public sealed record NumberExpression(decimal Value) : IExpression
{
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.Visit(this);
}

public sealed record AddExpression(IExpression Left, IExpression Right) : IExpression
{
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.Visit(this);
}

public sealed record MultiplyExpression(IExpression Left, IExpression Right) : IExpression
{
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.Visit(this);
}

public interface IExpressionVisitor<T>
{
    T Visit(NumberExpression expression);
    T Visit(AddExpression expression);
    T Visit(MultiplyExpression expression);
}

public sealed class EvaluateVisitor : IExpressionVisitor<decimal>
{
    public decimal Visit(NumberExpression expression) => expression.Value;

    public decimal Visit(AddExpression expression) =>
        expression.Left.Accept(this) + expression.Right.Accept(this);

    public decimal Visit(MultiplyExpression expression) =>
        expression.Left.Accept(this) * expression.Right.Accept(this);
}

public sealed class PrintVisitor : IExpressionVisitor<string>
{
    public string Visit(NumberExpression expression) => expression.Value.ToString();

    public string Visit(AddExpression expression) =>
        $"({expression.Left.Accept(this)} + {expression.Right.Accept(this)})";

    public string Visit(MultiplyExpression expression) =>
        $"({expression.Left.Accept(this)} * {expression.Right.Accept(this)})";
}
```

Usage:

```csharp
IExpression expression = new AddExpression(
    new NumberExpression(2),
    new MultiplyExpression(new NumberExpression(3), new NumberExpression(4)));

Console.WriteLine(expression.Accept(new PrintVisitor()));    // (2 + (3 * 4))
Console.WriteLine(expression.Accept(new EvaluateVisitor())); // 14
```

**In .NET**

- Roslyn syntax tree visitors (`CSharpSyntaxVisitor<T>`).
- `System.Linq.Expressions.ExpressionVisitor`.
- Roslyn analyzers and code fixers that walk syntax trees.

**Visitor vs pattern matching:** if the type set is small and closed, a `switch`
expression over an abstract record hierarchy is simpler than Visitor. C# has no
built-in discriminated unions, so the compiler does not check that the switch
covers every case. Use Visitor when operations multiply and live in separate
assemblies.

---

## Patterns already in the framework

You rarely need to implement these from scratch. Recognize them so you reuse the
platform instead of reinventing it.

| Pattern | .NET building blocks |
| --- | --- |
| Singleton | `AddSingleton`, `Lazy<T>` |
| Factory Method | `ILoggerFactory`, `DbProviderFactory`, `Activator.CreateInstance`, `IHttpClientFactory` |
| Abstract Factory | `DbProviderFactory`, provider families behind one abstraction |
| Builder | `StringBuilder`, `HostBuilder`, `ConfigurationBuilder`, `UriBuilder` |
| Prototype | `record` + `with`, copy constructors |
| Adapter | `StreamReader` and `StreamWriter`, `Enumerable.Cast<T>` |
| Bridge | `ILogger` + providers, ADO.NET provider model |
| Composite | `Expression` trees, `XElement`, UI control trees |
| Decorator | `Stream` wrappers, ASP.NET Core middleware, `DelegatingHandler` |
| Facade | `HttpClient`, `File`, application services |
| Flyweight | string interning, `Encoding` caches |
| Proxy | `DispatchProxy`, EF Core lazy loading, mocking frameworks |
| Chain of Responsibility | middleware, `DelegatingHandler` |
| Command | `System.Windows.Input.ICommand` (WPF), `System.CommandLine.Command` (CLI), MediatR requests |
| Interpreter | `Expression` trees, `Regex` |
| Iterator | `IEnumerable<T>`, `yield return`, `IAsyncEnumerable<T>` |
| Mediator | MediatR, SignalR hubs, dialog coordination |
| Memento | `record` snapshots, undo stacks, `ChangeTracker` |
| Observer | `event`, `IObservable<T>`, `INotifyPropertyChanged`, `IOptionsMonitor` |
| State | `Stream` state, state machine libraries |
| Strategy | `IComparer<T>`, `IEqualityComparer<T>`, `ICompressionProvider` |
| Template Method | `Stream` and `TextWriter`, `Controller.OnActionExecutionAsync` |
| Visitor | `ExpressionVisitor`, Roslyn syntax visitors |

---

## Architectural patterns used in .NET apps

These are not GoF patterns. They are larger-scale patterns you will meet in
production .NET code.

### Dependency Injection and Composition Root

Register abstractions in one place (`Program.cs`) and inject them. This is the
default pattern in ASP.NET Core and the generic host.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddSingleton<IClock, SystemClock>();

var app = builder.Build();
app.MapGet("/orders/{id:int}", (int id, OrderService service) => service.Get(id));
app.Run();
```

Rules of thumb:

- Constructor injection only. Avoid service locator.
- Configure the container once in the composition root. Do not call `BuildServiceProvider` inside
  business code.
- Prefer `Scoped` for request-bound work, `Singleton` for stateless or shared
  services, `Transient` for cheap stateful objects.

### Options pattern

Strongly typed configuration instead of magic strings.

```csharp
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
}
```

Register the options in `Program.cs` before `builder.Build()`:

```csharp
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
```

Consume with `IOptions<T>`, `IOptionsSnapshot<T>` (scoped), or `IOptionsMonitor<T>`
(singleton + change notifications).

### Repository and Unit of Work

Wrap persistence behind interfaces and commit changes as one unit.

```csharp
public interface IOrderRepository
{
    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

EF Core's `DbContext` is already a unit of work and its `DbSet<T>` is close to a
repository. Add repositories when you need to isolate the ORM, test without a
database, or enforce aggregate boundaries. Do not add them by default.

### Specification

Encapsulate query criteria in composable objects.

```csharp
using System.Linq.Expressions;

public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity) => ToExpression().Compile()(entity);

    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);
}

internal sealed class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var left = _left.ToExpression();
        var right = _right.ToExpression();
        var parameter = Expression.Parameter(typeof(T), "entity");
        var leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body);
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body);
        var body = Expression.AndAlso(leftBody, rightBody);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _from;
    private readonly ParameterExpression _to;

    public ParameterReplacer(ParameterExpression from, ParameterExpression to)
    {
        _from = from;
        _to = to;
    }

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _from ? _to : base.VisitParameter(node);
}
```

The parameter replacer keeps the composed expression free of `Expression.Invoke`,
which query providers often cannot translate.

### Result and Either

Avoid exceptions for expected failures.

```csharp
public abstract record Result<T>
{
    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(string Error) : Result<T>;
}
```

Usage:

```csharp
Result<int> Parse(string text) =>
    int.TryParse(text, out var value)
        ? new Result<int>.Success(value)
        : new Result<int>.Failure("Not a number.");

var result = Parse("42");
Console.WriteLine(result is Result<int>.Success { Value: var number } ? number : 0); // 42
```

### CQRS and messaging

Separate writes (commands) from reads (queries). At small scale, use separate
handler classes. At larger scale, CQRS brings separate read models,
eventual consistency, and messaging (MassTransit, NServiceBus, RabbitMQ, Azure
Service Bus, Kafka).

### Outbox and inbox

Persist outgoing messages in the same transaction as domain changes, then publish
them reliably. The inbox side records consumed message IDs so a retried delivery
does not apply the same change twice. Use both when database writes and message
publication must not diverge.

### Pipeline and middleware

Compose cross-cutting behavior as a sequence of handlers (ASP.NET Core
middleware, MediatR pipeline behaviors, `DelegatingHandler`). This is the
Chain of Responsibility pattern applied at application scale.

### Saga and process manager

Coordinate multi-step, long-running workflows with compensating actions instead
of a distributed transaction. Relevant for workflows that span services, such as
Durable Functions orchestrations.

---

## Quick reference table

| Pattern | Category | One-liner | Typical .NET use |
| --- | --- | --- | --- |
| Singleton | Creational | One shared instance | `AddSingleton`, caches |
| Factory Method | Creational | Delegate creation | `ILoggerFactory`, providers |
| Abstract Factory | Creational | Families of products | `DbProviderFactory` |
| Builder | Creational | Step-by-step construction | `StringBuilder`, host builder |
| Prototype | Creational | Clone existing object | `record with` |
| Adapter | Structural | Convert an interface | Vendor SDK wrappers |
| Bridge | Structural | Separate abstraction and implementation | `ILogger` + providers |
| Composite | Structural | Treat trees uniformly | `Expression` trees, UI |
| Decorator | Structural | Wrap to add behavior | Streams, middleware |
| Facade | Structural | Simplify a subsystem | `HttpClient`, services |
| Flyweight | Structural | Share intrinsic state | string interning, shared caches |
| Proxy | Structural | Control access | `DispatchProxy`, lazy loading |
| Chain of Responsibility | Behavioral | Pass request along handlers | Middleware, handlers |
| Command | Behavioral | Request as object | `ICommand`, CLI commands, MediatR |
| Interpreter | Behavioral | Evaluate a small language | Expression trees, rules |
| Iterator | Behavioral | Sequential traversal | `IEnumerable<T>`, `yield` |
| Mediator | Behavioral | Central communication hub | MediatR, SignalR |
| Memento | Behavioral | Snapshot and restore | Undo stacks, snapshots |
| Observer | Behavioral | Notify subscribers | events, `IObservable<T>` |
| State | Behavioral | Behavior changes with state | Order workflows |
| Strategy | Behavioral | Swap algorithms | `IComparer<T>`, policies |
| Template Method | Behavioral | Fixed skeleton, overridable steps | Base classes, action filters |
| Visitor | Behavioral | New operations on stable types | Expression visitors |

---

## Choosing a pattern

Ask these questions in order:

1. **Is a simpler construct enough?** A delegate, `record`, local function, or
   plain interface often replaces a pattern.
2. **What varies?** Creation maps to creational patterns, composition to
   structural patterns, and behavior or communication to behavioral patterns.
3. **Who owns the variation?** If the caller chooses per call, inject a Strategy
   or use a Factory. If the object changes over time, use State.
4. **Is the structure stable but operations growing?** Visitor.
5. **Is the interface incompatible?** Adapter. Same interface but extra behavior?
   Decorator. Same interface but access control? Proxy.
6. **Is the hierarchy a tree?** Composite.
7. **Are there too many constructor parameters?** Builder.
8. **Is there a fixed workflow with variants?** Template Method.

Prefer composition over inheritance. Prefer injection over static access. Prefer
explicit code over pattern ceremony.

---

## Common pitfalls

- **Pattern worship.** Adding interfaces, factories, and layers with one
  implementation each. Wait for the second or third variation.
- **Singleton as a service locator.** Hiding dependencies makes tests order-dependent
  and hides the real graph.
- **Anemic strategy factories.** A `switch` that constructs strategies on every
  call usually belongs in DI keyed services.
- **Decorator order bugs.** Caching outside logging behaves differently from
  logging outside caching. Document and test the order.
- **Leaky abstractions.** A repository that returns `IQueryable<T>` is not
  isolating the ORM.
- **Events without unsubscription.** Long-lived publishers keep subscribers alive.
- **Visitor rigidity.** Every new element type requires touching every visitor.
  Use pattern matching for closed, small hierarchies.
- **Interpreter overreach.** Hand-written parsers become unmaintainable. Use a
  parser generator.
- **Async in constructors.** Factories should return `Task<T>` or use an explicit
  `InitializeAsync`, never block in a constructor.
- **Thread safety assumptions.** Singletons and caches need explicit concurrency
  design. `Dictionary` is not thread-safe for writes, or for a write during a read.
- **Ignoring cancellation.** Pass `CancellationToken` through all async layers.

---

## Further reading

- *Design Patterns: Elements of Reusable Object-Oriented Software* by Gamma, Helm,
  Johnson, and Vlissides.
- *Head First Design Patterns* by Freeman and Robson.
- *Patterns of Enterprise Application Architecture* by Martin Fowler.
- *Dependency Injection Principles, Practices, and Patterns* by Seemann and van Deursen.
- .NET documentation: <https://learn.microsoft.com/dotnet/standard/>
- .NET runtime source: <https://github.com/dotnet/runtime>
