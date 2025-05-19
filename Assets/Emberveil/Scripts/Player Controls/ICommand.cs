using System;

public interface ICommand
{
    bool CanExecute();
    void Execute();
}

public class RelayCommand : ICommand
{
    private readonly Func<bool> canExecute;
    private readonly Action execute;

    public RelayCommand(Func<bool> canExecute, Action execute)
    {
        this.canExecute = canExecute;
        this.execute = execute;
    }

    public bool CanExecute() => canExecute();
    public void Execute() => execute();
}
