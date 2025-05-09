using System;

namespace Domain;

public class DotVisioException : Exception
{
    public DotVisioException()
    {
    }

    public DotVisioException(string message) : base(message)
    {
    }

    public DotVisioException(string message, Exception inner) : base(message, inner)
    {
    }
}