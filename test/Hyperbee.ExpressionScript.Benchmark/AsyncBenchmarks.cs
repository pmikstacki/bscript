using BenchmarkDotNet.Attributes;

namespace Hyperbee.ExpressionScript.Benchmark;

public class ScriptBenchmarks
{

    [GlobalSetup]
    public void Setup()
    {

    }

    // Compile

    [BenchmarkCategory( "Compile" )]
    [Benchmark( Description = "Hyperbee Compile" )]
    public void Hyperbee_AsyncBlock_Compile()
    {
        //var script = @"
        //    let result = if (x > 10) {
        //            x * 2;
        //        } else {
        //            x - 2;
        //        };";

        // ExpressionScript.Compile( script );
    }


    // Execute

    [BenchmarkCategory( "Execute" )]
    [Benchmark( Description = "Native Execute", Baseline = true )]
    public void Native_Execute()
    {
        NativeTestAsync();
    }

    [BenchmarkCategory( "Execute" )]
    [Benchmark( Description = "Hyperbee Execute" )]
    public void Hyperbee_Script_Execute()
    {
        //var script = @"
        //    let result = if (x > 10) {
        //            x * 2;
        //        } else {
        //            x - 2;
        //        };";

        // ExpressionScript.Execute( script );
    }

    // Helpers

    public static int NativeTestAsync()
    {
        var i = 1;
        if ( true )
        {
            i++;
        }

        return i;
    }
}
