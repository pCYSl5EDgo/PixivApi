namespace PixivApi.Core;

public interface IOverwrite<T>
{
    void Overwrite(T source);
}
