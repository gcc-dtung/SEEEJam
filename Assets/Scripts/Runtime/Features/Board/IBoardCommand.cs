public interface IBoardCommand
{
    bool Execute();
    bool Undo();
}
