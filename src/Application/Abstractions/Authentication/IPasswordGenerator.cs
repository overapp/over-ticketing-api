namespace Application.Abstractions.Authentication;

public interface IPasswordGenerator
{
    string Generate(int length = 16);
}
