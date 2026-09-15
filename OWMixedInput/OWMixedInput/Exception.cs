namespace OWMixedInput;

// A custom exception types.
// We use these both to make errors coming from this mod
// and because dotnet4 does not have enough useful exceptions.

public class Exception: System.Exception {
    public Exception () : base() {}
    public Exception (string message) : base(message) {}
}

public class Unreachable: Exception {
    public Unreachable () : base() {}
    public Unreachable (string message) : base(message) {}
}

public class Unimplemented: Exception {
    public Unimplemented () : base() {}
    public Unimplemented (string message) : base(message) {}
}
