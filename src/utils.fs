/// Non biology routes that are helpful for general process / data ops, some bio IO routines
module utils

open System
open System.Text

type Platform = UNIX | NOTUNIX | FABLE
#if FABLE_COMPILER
let platform = FABLE
#else
open System.IO
let cmdPath = @"c:\WINDOWS\system32\cmd.exe"
let bashPath = "/bin/bash"

/// Get CLR platform
let platform =
    match (Environment.OSVersion.Platform) |> int with
        | 4 | 6 | 128 -> UNIX
        | _ -> NOTUNIX

let argv() = System.Environment.GetCommandLineArgs()

let md5 = System.Security.Cryptography.MD5.Create()

(*

/// Calculate an md5 based checksum given a string
let hash (s:string) =
    md5.Initialize()
    let b = Encoding.UTF8.GetBytes(s)
    let hash = md5.ComputeHash(b)
    hash |> Seq.map (fun b -> sprintf "%02x" b) |> String.concat ""
    
*)
#endif
//
// ------------------------------------------------------------
// File name utils
//

/// Split path up and extract base file name
let baseName (path : string) =
    let x = path.Split([| ( match platform with | FABLE | UNIX -> '/' | NOTUNIX -> '\\')  |])

    match x.[x.Length-1] with
        | "" -> x.[x.Length-2]
        | _ as y -> y

#if !FABLE_COMPILER
(*

/// Get directory part of a path
let baseDir (path : string) =
    let sep = ( match platform with | FABLE | UNIX -> '/' | NOTUNIX -> '\\') 

    let x = path.Split([| sep |])

    match x.[x.Length-1] with
        | "" -> String.Join(sprintf "%c" sep,x)
        | _  -> String.Join(sprintf "%c" sep,x.[..x.Length-2]   )

/// join directory and filename
let opj (dir:string) file = Path.Combine(dir,file)
*)
#endif

(*
    
    if dir.EndsWith(match platform with | UNIX -> "/" | NOTUNIX -> @"\") then
        sprintf @"%s%s" dir file
    else
        sprintf @"%s\%s" dir file
*)

/// convert damn forward slashes

let smashSlash (s:string) =
    match platform with
        | FABLE | UNIX -> s.Replace(@"\","/")
        | NOTUNIX -> s.Replace("/",@"\")


/// Change the file .suffix  newSuffix file :  New suffix specified without dot
let replaceSuffix newSuffix (file:string)  =
    let dotIndex = file.LastIndexOf('.')
    let finalSuffix = if newSuffix = "" then "" else "." + newSuffix
    match dotIndex with
        | -1 -> (file+finalSuffix)
        | _ -> 
            let prefix = file.Substring(0,dotIndex)
            (prefix+finalSuffix)



#if !FABLE_COMPILER
(*


open System.Diagnostics

/// Run a specific command or sequence of commands piped (cmd syntax)
let runProc cmdline =
    
    let args =
        (match platform with | UNIX -> "-c \"" | NOTUNIX -> "/c ")
        + cmdline
        + (match platform with | UNIX -> "\"" | NOTUNIX -> "")
    
    //let p = Process.Start((match platform with | UNIX -> bashPath | NOTUNIX -> cmdPath), args)
    let p = new Process()
    p.StartInfo.FileName <- (match platform with | UNIX -> bashPath | NOTUNIX -> cmdPath)
    p.StartInfo.Arguments <- args
    p.StartInfo.CreateNoWindow <- true
    p.StartInfo.WindowStyle <- System.Diagnostics.ProcessWindowStyle.Hidden
    p.Start() |> ignore

    p.WaitForExit()
    if p.ExitCode <> 0 then
        printf "ERROR: runProc: exitCode=%d\n" p.ExitCode
    else
        printf "Run %s ok\n" cmdline
    p.ExitCode


/// Run process in background.  Path to binary, args as string and ( echo stdout true/false [unimp] )
/// version 3, async output handling
let runProc2 path args _ (* echoStdout *)=
    use p = new Process()
    p.StartInfo.FileName <- path
    p.StartInfo.Arguments <- args
    p.StartInfo.CreateNoWindow <- false
    p.StartInfo.RedirectStandardError <- true
    p.StartInfo.RedirectStandardOutput <- true
    p.StartInfo.WindowStyle <- System.Diagnostics.ProcessWindowStyle.Hidden
    p.StartInfo.UseShellExecute <- false
    if not (p.Start()) then
        999,"","Failed to exec"
    else

        let sbOut = new StringBuilder()
        let sbErr = new StringBuilder()
        p.OutputDataReceived.Add(fun ev -> sbOut.Append(ev.Data).Append("\n")|> ignore)
        p.ErrorDataReceived.Add(fun ev -> sbOut.Append(ev.Data).Append("\n") |> ignore)

        p.BeginErrorReadLine()
        p.BeginOutputReadLine()
        p.WaitForExit()
        let stdErr = sbErr.ToString()
        if stdErr <> "" then
            printfn "ERROR:\n%s\n" stdErr
        p.ExitCode,sbOut.ToString(),stdErr

/// Returns files in dir ending with one of a list of suffices
let filesEndingWith (inDir : string) (suffixList : string list) =
    Directory.GetFiles(inDir)
    |> Seq.filter (fun name -> List.exists (fun (suffix : string) -> name.EndsWith(suffix)) suffixList)

/// returns each line in the provided stream
let eachLineInStream (f:StreamReader) = 
    let ok = ref true
    seq {
        while !ok do
            let x =  f.ReadLine()
            if x <> null then 
                yield x 
                if f.EndOfStream then 
                    f.Close()
                    ok := false
            else
                f.Close()
                ok := false

            }

/// Equivalent of eachLineIn for std input
let eachStdinLine() =
    seq {  
            let eof = ref false
            while not !eof do
                match stdin.ReadLine() with
                    | null -> eof := true
                    | _ as x -> yield x
        } 

/// Does NOT support GZIP compression any more, because .NET Gzip implementation does not
/// support concatenation of multiple gzip files (http://community.sharpdevelop.net/forums/t/9121.aspx)
///  Use this code instead:
///   open ICSharpCode.SharpZipLib.GZip
///   open System.IO
///   let fz = new GZipInputStream(new FileStream(file, FileMode.Open, FileAccess.Read))
///   let stream = new StreamReader(fz)
///   eachLineInStream(stream)
let eachLineIn (file:string) =
    let f = 
//            if file.EndsWith(".gz") then
//                let fz = new GZipStream(new FileStream(file, FileMode.Open, FileAccess.Read), CompressionMode.Decompress)
//                new StreamReader(fz)
//            else 
            new StreamReader(file)
    eachLineInStream(f)


let fourAtATimeInStreamReader (f:StreamReader) = 
    let ok = ref true
    seq {
        while !ok do
            let rl = f.ReadLine
            let x = rl()
            if x <> null then
                yield [| x ; rl() ; rl() ; rl() |]
                if f.EndOfStream then 
                    f.Close()
                    ok := false
            else
                f.Close()
                ok := false
        }

/// Does NOT support GZIP compression any more, because .NET Gzip implementation does not
/// support concatenation of multiple gzip files (http://community.sharpdevelop.net/forums/t/9121.aspx)
///  Use this code instead:
///   open ICSharpCode.SharpZipLib.GZip
///   open System.IO
///   let fz = new GZipInputStream(new FileStream(file, FileMode.Open, FileAccess.Read))
///   let stream = new StreamReader(fz)
///   fourAtATimeInStreamReader(stream)
let fourAtATime (file:string) = 
    let f = 
//            if file.EndsWith(".gz") then
//                let fz = new GZipStream(new FileStream(file, FileMode.Open, FileAccess.Read), CompressionMode.Decompress)
//                new StreamReader(fz)
//            else 
        new StreamReader(file)
    fourAtATimeInStreamReader f

    


let tabSplit (line:string) = line.Split([|'\t'|])
let ts x = Seq.map tabSplit x

let countLines file =  Seq.fold (fun count _(*line*) -> count + 1) 0 (eachLineIn file) 

// Registry utils
// Registry handling    
(*
open Microsoft.Win32

let ensureAmyrisKey () =  
    let key = Registry.CurrentUser.OpenSubKey("Software",true)
    match key.OpenSubKey("Amyris",true) with
        | null ->
            key.CreateSubKey("Amyris")
        | anyKey -> anyKey


let getSetKey (key:RegistryKey) keyName keyValue =
    match key.GetValue(keyName) with
        | :? string as x -> x
        | _ -> 
            key.SetValue(keyName,keyValue)
            keyValue

*)
*)
#endif
// Sequence handling routines
/// Reverse complement a DNA sequence
/// This version is deprecated, please use biolib.revComp instead
[<Obsolete>]
let revCompDeprecated (bases : char []) =
    let rcBase x =
        match x with
        | 'T'  -> 'A'
        | 't' -> 'a'
        | 'C'  -> 'G'
        | 'c' -> 'g'
        | 'A' -> 'T'
        | 'a'  -> 't'
        | 'G'  -> 'C'
        | 'g' -> 'c'
        | 'N'-> 'N'
        | 'n'  -> 'n'
        | ' ' -> ' '
        | '\n' -> ' '
        | '\r' -> '\r'
        | '-' -> '-'
        | _ -> failwith $"bad base '{x}' in rcBase"

    let comp = Array.map (rcBase) bases
    Array.rev(comp)

let arr2seq (arr:char[]) = new string(arr)

//    let s = System.Text.StringBuilder()
//    (Array.fold (fun (str:System.Text.StringBuilder) (c:char) -> str.Append(c) ) s arr).ToString()

/// Wrap DNA sequence at 60 chars per line
let format60 (a:char[]) =
    if a.Length = 0 then ""
    else
        let newLinesRequired = ((a.Length - 1) / 60) |> max 0
        let res = Array.create (a.Length + newLinesRequired) '\n'

        /// i = index into input array
        /// j = index into output array
        /// n = character count % 60
        let rec move i j n =
            if i = a.Length then
                res |> arr2seq
            elif n = 60 then
                move i (j+1) 0
            else
                res.[j]<-a.[i]
                move (i+1) (j+1) (n+1)
        move 0 0 0

#if !FABLE_COMPILER
(*


/// Given a file path f with contents
/// key = value
/// produce a dictionary.  # lines are comments
/// and key, value are trimmed.
/// Duplicates are not allowed
let loadConfig f =
    eachLineIn f  
    |> Seq.filter(fun line -> not (line.StartsWith("#")))  
    |> Seq.choose (fun line ->
        match line.IndexOf('=') with
        | -1 -> None
        |i ->
            let key = line.[..(i-1)].Trim()
            let value = 
                if line.Length > i + 1 then
                    line.[i+1..].Trim()
                else
                    "" 
            Some(key,value)) 
    |> Map.ofSeq

// File format handling
type BP = { bp : char ; depth : int }

let parsePileup pileup = 
    let currChr,currArray,accum = 
        eachLineIn pileup
        |> Seq.fold
            (fun (currChrom,currArray : ResizeArray<BP>,accum) (line:string) -> 
                let cols = line.Split([|'\t'|])
                match cols with
                | [| chr ; _(*pos*) ; bp ; depth ; _(*pileup*) |] ->
                
                    if chr = currChrom then
                        currArray.Add( {bp = bp.[0] ; depth = int(depth) } )
                        (currChrom,currArray,accum)
                    else
                        // New chromosome
                        printfn "Starting chromosome %s" chr
                        let newArr = new ResizeArray<BP>()

                        newArr.Add( {bp = bp.[0] ; depth = int(depth) } )
                    
                        if currChrom = "" then
                            (chr,newArr,[])
                        else
                        
                            (chr, newArr , (currChrom,(currArray.ToArray()) ) :: accum)
                | _->
                    printf "%A" cols
                    failwith "line with illegal # cols :( ")
            ("",new ResizeArray<BP>() , [] ) 

    (currChr, (currArray.ToArray()))::accum 

*)
#endif
/// Return true if two character sequences are identical.
let compareSlices seqA seqB =
    match (Seq.compareWith Operators.compare seqA seqB) with
    | 0 -> true
    | _ -> false

open System.Collections.Generic

/// Zip two sequences of dissimilar length, padding missing entries in the shorter sequence
/// with None.
let zipWithPad (a: seq<'T>) (b: seq<'U>) =
    let generator (en1: IEnumerator<'T>, en2: IEnumerator<'U>) =
        match en1.MoveNext(), en2.MoveNext() with
        | true, true ->
            let v = Some(en1.Current), Some(en2.Current)
            Some(v, (en1, en2))
        | true, false ->
            let v = Some(en1.Current), None
            Some(v, (en1, en2))
        | false, true ->
            let v = None, Some(en2.Current)
            Some(v, (en1, en2))
        | false, false ->
            None
    use en1 = a.GetEnumerator()
    use en2 = b.GetEnumerator()
    Seq.unfold generator (en1, en2)
