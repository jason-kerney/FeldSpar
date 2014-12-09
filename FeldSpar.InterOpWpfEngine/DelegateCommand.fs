namespace FeldSpar.Api.Engine.ClrInterop.ViewModels
open System
open System.Windows.Input

/// <summary>
/// An implementation of ICommand for use in WPF
/// Thank you: http://wpftutorial.net/DelegateCommand.html
/// </summary>
type DelegateCommand (execute:Action<obj>, canExecute:Predicate<obj>) =
    let notify = new Event<_, _>()


    interface ICommand with
        [<CLIEvent>]
        member this.CanExecuteChanged = notify.Publish

        member this.CanExecute param =
            if canExecute = null then true
            else canExecute.Invoke param

        member this.Execute param = execute.Invoke param

    new (execute:Action<obj>) = DelegateCommand(execute, fun _ -> true)


    member this.RaiseCanExecuteChanged () = notify.Trigger(this, EventArgs.Empty)
