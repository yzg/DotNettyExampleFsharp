// F# の詳細については、http://fsharp.org を参照してください
// 詳細については、'F# チュートリアル' プロジェクトを参照してください。

open System.Threading.Tasks
open DotNetty.Common.Internal.Logging
open DotNetty.Handlers.Logging
open DotNetty.Handlers.Tls
open DotNetty.Transport.Bootstrapping
open DotNetty.Transport.Channels
open DotNetty.Transport.Channels.Sockets
open System
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.Console

type DiscardServerHandler() =
    inherit SimpleChannelInboundHandler<obj>()

    override this.ChannelRead0(context : IChannelHandlerContext, message : obj) =
        printfn "read!"
        ()

    override this.ExceptionCaught(ctx : IChannelHandlerContext, e : Exception) =
        printfn "%A" e
        ctx.CloseAsync () |> ignore

let setConsoleLogger () =
    InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider(Func<string, LogLevel, bool>(fun s  level -> true), false))
    
    
let runServerAsync () = async {
    setConsoleLogger ()

    let bossGroup = new MultithreadEventLoopGroup(1)
    let workerGroup = new MultithreadEventLoopGroup()
    //if ServerSettings.IsSsl then

    try
        let bootstrap = new ServerBootstrap()
        bootstrap
            .Group(bossGroup, workerGroup)
            .Channel<TcpServerSocketChannel>()
            .Option(ChannelOption.SoBacklog, 100)
            .Handler(new LoggingHandler("LSTN"))
            .ChildHandler(new ActionChannelInitializer<ISocketChannel>(fun channel ->
                let pipeline = channel.Pipeline
                // if (tlCertificate)

                pipeline.AddLast(new LoggingHandler("CONN")) |> ignore

                pipeline.AddLast(new DiscardServerHandler()) |> ignore
            ))
            |> ignore
        let! bootstrapChannel = Async.AwaitTask(bootstrap.BindAsync(8007))

        Console.ReadLine() |> ignore

        do! Async.AwaitTask(bootstrapChannel.CloseAsync())

    finally
        Task.WaitAll([|bossGroup.ShutdownGracefullyAsync(); workerGroup.ShutdownGracefullyAsync()|])
        

}

[<EntryPoint>]
let main argv = 
    (runServerAsync())
    |> Async.RunSynchronously
    0 // 整数の終了コードを返します
