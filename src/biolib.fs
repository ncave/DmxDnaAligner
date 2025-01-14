/// Basic biology operations for sequence reading, translation and blosum ops
module biolib

open System
open System.IO
open System.Text
open System.Collections.Generic
open utils

let bases = ['A' ; 'T' ; 'C' ; 'G']

let codons = seq {  for a in bases do
                        for b in bases do
                            for c in bases -> [| a ; b ; c |]  } |> Array.ofSeq
                
let codon2aa (bases : char []) =
    if bases.[0] = 'N' || bases.[1] = 'N' || bases.[2] = 'N' then '?'
    else
        let baseMap x =
                match x with
                    | 'T' | 't' -> 0
                    | 'C' | 'c' -> 1
                    | 'A' | 'a' -> 2
                    | 'G' | 'g' -> 3
                    | _ -> 
                        failwith "XXX" // (sprintf "bad base '%c'in codon2aa " x)
    
        let i = (baseMap bases.[0]) * 16 + (baseMap bases.[1]) * 4 + (baseMap bases.[2])                
        let aa = "FFLLSSSSYY**CC*WLLLLPPPPHHQQRRRRIIIMTTTTNNKKSSRRVVVVAAAADDEEGGGG"
        aa.[i]

/// Symbolic trigram representations of amino acids
type AminoAcidSymbols = Arg|His|Lys|Asp|Glu|Ser|Thr|Asn|Gln|Cys|Sec|Gly|Pro|Ala|Val|Ile|Leu|Met|Phe|Tyr|Trp|End

type AminoAcidProperties = { trigram : string ; letter : char ; hydrophobicity : float option}

// Amino acid scale: Normalized consensus hydrophobicity scale.
// Author(s): Eisenberg D., Schwarz E., Komarony M., Wall R.
// Reference: J. Mol. Biol. 179:125-142(1984).
// https://web.expasy.org/protscale/pscale/Hphob.Eisenberg.html

let aminoAcidProperties = 
    [
        (Ala, { trigram="Ala" ; letter = 'A' ; hydrophobicity = Some 0.620 })
        (Arg, { trigram="Arg" ; letter = 'R' ; hydrophobicity = Some -2.530 })
        (Asn, { trigram="Asn" ; letter = 'N' ; hydrophobicity = Some -0.780 })
        (Asp, { trigram="Asp" ; letter = 'D' ; hydrophobicity = Some -0.900 })
        (Cys, { trigram="Cys" ; letter = 'C' ; hydrophobicity = Some 0.290 })
        (Gln, { trigram="Gln" ; letter = 'Q' ; hydrophobicity = Some -0.850 })
        (Glu, { trigram="Glu" ; letter = 'E' ; hydrophobicity = Some -0.740 })
        (Gly, { trigram="Gly" ; letter = 'G' ; hydrophobicity = Some 0.480 })
        (His, { trigram="His" ; letter = 'H' ; hydrophobicity = Some -0.400 })
        (Ile, { trigram="Ile" ; letter = 'I' ; hydrophobicity = Some 1.380 })
        (Leu, { trigram="Leu" ; letter = 'L' ; hydrophobicity = Some 1.060 })
        (Lys, { trigram="Lys" ; letter = 'K' ; hydrophobicity = Some -1.500 })
        (Met, { trigram="Met" ; letter = 'M' ; hydrophobicity = Some 0.640 })
        (Phe, { trigram="Phe" ; letter = 'F' ; hydrophobicity = Some 1.190 })
        (Pro, { trigram="Pro" ; letter = 'P' ; hydrophobicity = Some 0.120 })
        (Ser, { trigram="Ser" ; letter = 'S' ; hydrophobicity = Some -0.180 })
        (Thr, { trigram="Thr" ; letter = 'T' ; hydrophobicity = Some -0.050 })
        (Trp, { trigram="Trp" ; letter = 'W' ; hydrophobicity = Some 0.810 })
        (Tyr, { trigram="Tyr" ; letter = 'Y' ; hydrophobicity = Some 0.260 })
        (Val, { trigram="Val" ; letter = 'V' ; hydrophobicity = Some 1.080 })
        (Sec, { trigram="Sec" ; letter = 'U' ; hydrophobicity = None }) // not documented int Eisenberg
        (End, { trigram="End" ; letter = '*' ; hydrophobicity = None })
    ] |> Map.ofList

let aaLetterToSymbolic (aa:char) =
    match aa with
        | 'R' -> Arg
        | 'H' -> His
        | 'K' -> Lys
        | 'D' -> Asp
        | 'E' -> Glu
        | 'S' -> Ser
        | 'T' -> Thr
        | 'N' -> Asn
        | 'Q' -> Gln
        | 'C' -> Cys
        | 'U' -> Sec
        | 'G' -> Gly
        | 'P' -> Pro
        | 'A' -> Ala
        | 'V' -> Val
        | 'I' -> Ile
        | 'L' -> Leu
        | 'M' -> Met
        | 'F' -> Phe
        | 'Y' -> Tyr
        | 'W' -> Trp
        | '*' -> End
        | _ -> failwith "XXX" // f "ERROR: unknown amino acid letter '%c' in aaLetterToSymbolic" aa


/// Translates amino acid represented with one character to a trigram, eg 'W' -> "Trp" , '*' -> "End"
let aaTrigramToSymbolic (aa:string) =
    match aa with 
    | "Arg" -> Arg
    | "His" -> His
    | "Lys" -> Lys
    | "Asp" -> Asp
    | "Glu" -> Glu
    | "Ser" -> Ser
    | "Thr" -> Thr
    | "Asn" -> Asn
    | "Gln" -> Gln
    | "Cys" -> Cys
    | "Sec" -> Sec
    | "Gly" -> Gly
    | "Pro" -> Pro
    | "Ala" -> Ala
    | "Val" -> Val
    | "Ile" -> Ile
    | "Leu" -> Leu
    | "Met" -> Met
    | "Phe" -> Phe
    | "Tyr" -> Tyr
    | "Trp" -> Trp
    | "End" -> End
    | _ -> failwith "XXX" // f "bad aa '%s' in aaLetterToSymbolic" aa

/// Translates amino acid represented with one character to a trigram, eg 'W' -> "Trp" , '*' -> "End"
let aaLetterToTrigram (aa:char) =
    match aa with 
    | 'R' -> "Arg"
    | 'H' -> "His"
    | 'K' -> "Lys"
    | 'D' -> "Asp"
    | 'E' -> "Glu"
    | 'S' -> "Ser"
    | 'T' -> "Thr"
    | 'N' -> "Asn"
    | 'Q' -> "Gln"
    | 'C' -> "Cys"
    | 'U' -> "Sec"
    | 'G' -> "Gly"
    | 'P' -> "Pro"
    | 'A' -> "Ala"
    | 'V' -> "Val"
    | 'I' -> "Ile"
    | 'L' -> "Leu"
    | 'M' -> "Met"
    | 'F' -> "Phe"
    | 'Y' -> "Tyr"
    | 'W' -> "Trp"
    | '*' -> "End"
    | _ -> failwith "XXX" //f "bad aa '%c'" aa

/// Check is the character is a proper DNA base
let isDnaBaseStrict x = 
    match x with 
    | 'T' | 't' | 'C' | 'c' | 'A' | 'a'| 'G' | 'g'  -> true
    | _ -> false

/// Check is the character is a proper or ambiguous DNA base
let isDnaBase x = 
    match x with 
    | 'T' | 'C' | 'A' | 'G' | 'N' | 'R' | 'Y' | 'S' | 'W' | 'K' | 'M' 
    | 'B' | 'D' | 'H' | 'V' | 't' | 'c' | 'a' | 'g' | 'n' | 'r' | 'y' 
    | 's' | 'w' | 'k' | 'm' | 'b' | 'd' | 'h' | 'v' -> true
    | _ -> false
    
/// Strict rev comp implementation
let rcBasesStrict x =
    match x with
    | 'T' | 't' -> 'A'
    | 'C' | 'c' -> 'G'
    | 'A' | 'a' -> 'T'
    | 'G' | 'g' -> 'C'
    | 'N' | 'n' -> 'N'
    | _ -> failwith "XYZ" // (sprintf "bad base '%c'in rcBase" x)

/// Reverse complement of fully ambiguous bases
let rcBase x =
    match x with
    | 'T' -> 'A'
    | 'C' -> 'G'
    | 'A' -> 'T'
    | 'G' -> 'C'
    | 'N' -> 'N'
    | 'R' -> 'Y' // ('A','G') -> TC
    | 'Y' -> 'R'
    | 'S' -> 'S'
    | 'W' -> 'W' // AT
    | 'K' -> 'M' // -> ('G','T')
    | 'M' -> 'K' //  -> ('A','C')
    | 'B' -> 'V' // .................C or G or T  -> CGA
    | 'D' -> 'H' //  .................A or G or T 
    | 'H' -> 'D' // .................A or C or T 
    | 'V' -> 'B' // .................A or C or G
    | 't' -> 'a'
    | 'c' -> 'g'
    | 'a' -> 't'
    | 'g' -> 'c'
    | 'n' -> 'n'
    | 'r' -> 'Y' // ('A','G') -> TC
    | 'y' -> 'R'
    | 's' -> 'S'
    | 'w' -> 'w' // AT
    | 'k' -> 'm' // -> ('G','T')
    | 'm' -> 'k' //  -> ('A','C')
    | 'b' -> 'v' // .................C or G or T  -> CGA
    | 'd' -> 'h' //  .................A or G or T 
    | 'h' -> 'd' // .................A or C or T 
    | 'v' -> 'b' // .................A or C or G
    | ' ' -> ' '
    | '\n' -> ' '
    | '\r' -> '\r'
    | '-' -> '-'
    | _ -> failwith "XYW" // (sprintf "bad base '%c'in rcBase" x)

/// Reverse complement a DNA sequence
let revComp (bases : char []) =
    let comp = Array.map (rcBase) bases
    Array.rev(comp)

/// Translate DNA to protein, 3 bases at a time, folding from
/// the left, only adding an AA when we are in a position modulo 3
let translate (seq : char []) =
    [| for i in 0..3..seq.Length-3 -> codon2aa (seq.[i..i+2] )|] 

    (*
    let _(*off*),p =
        seq
        |> Array.fold
            (fun (i,prot) _ -> 
                if i % 3 = 0 && i < (seq.Length - 2) then
                    (i+1,Array.append prot [|codon2aa (seq.[i..i+2]) |] )
                else
                    (i+1,prot))
            (0,[||])
    p
    *)
    
// -----------------------------------------------
// Use a blosum matrix to evaluate the cost of a substitution

let b62 = "#
#  Matrix made by matblas from blosum62.iij
#  * column uses minimum score
#  BLOSUM Clustered Scoring Matrix in 1/2 Bit Units
#  Blocks Database = /data/blocks_5.0/blocks.dat
#  Cluster Percentage: >= 62
#  Entropy =   0.6979, Expected =  -0.5209
A  R  N  D  C  Q  E  G  H  I  L  K  M  F  P  S  T  W  Y  V  B  Z  X  *
A  4 -1 -2 -2  0 -1 -1  0 -2 -1 -1 -1 -1 -2 -1  1  0 -3 -2  0 -2 -1  0 -4 
R -1  5  0 -2 -3  1  0 -2  0 -3 -2  2 -1 -3 -2 -1 -1 -3 -2 -3 -1  0 -1 -4 
N -2  0  6  1 -3  0  0  0  1 -3 -3  0 -2 -3 -2  1  0 -4 -2 -3  3  0 -1 -4 
D -2 -2  1  6 -3  0  2 -1 -1 -3 -4 -1 -3 -3 -1  0 -1 -4 -3 -3  4  1 -1 -4 
C  0 -3 -3 -3  9 -3 -4 -3 -3 -1 -1 -3 -1 -2 -3 -1 -1 -2 -2 -1 -3 -3 -2 -4 
Q -1  1  0  0 -3  5  2 -2  0 -3 -2  1  0 -3 -1  0 -1 -2 -1 -2  0  3 -1 -4 
E -1  0  0  2 -4  2  5 -2  0 -3 -3  1 -2 -3 -1  0 -1 -3 -2 -2  1  4 -1 -4 
G  0 -2  0 -1 -3 -2 -2  6 -2 -4 -4 -2 -3 -3 -2  0 -2 -2 -3 -3 -1 -2 -1 -4 
H -2  0  1 -1 -3  0  0 -2  8 -3 -3 -1 -2 -1 -2 -1 -2 -2  2 -3  0  0 -1 -4 
I -1 -3 -3 -3 -1 -3 -3 -4 -3  4  2 -3  1  0 -3 -2 -1 -3 -1  3 -3 -3 -1 -4 
L -1 -2 -3 -4 -1 -2 -3 -4 -3  2  4 -2  2  0 -3 -2 -1 -2 -1  1 -4 -3 -1 -4 
K -1  2  0 -1 -3  1  1 -2 -1 -3 -2  5 -1 -3 -1  0 -1 -3 -2 -2  0  1 -1 -4 
M -1 -1 -2 -3 -1  0 -2 -3 -2  1  2 -1  5  0 -2 -1 -1 -1 -1  1 -3 -1 -1 -4 
F -2 -3 -3 -3 -2 -3 -3 -3 -1  0  0 -3  0  6 -4 -2 -2  1  3 -1 -3 -3 -1 -4 
P -1 -2 -2 -1 -3 -1 -1 -2 -2 -3 -3 -1 -2 -4  7 -1 -1 -4 -3 -2 -2 -1 -2 -4 
S  1 -1  1  0 -1  0  0  0 -1 -2 -2  0 -1 -2 -1  4  1 -3 -2 -2  0  0  0 -4 
T  0 -1  0 -1 -1 -1 -1 -2 -2 -1 -1 -1 -1 -2 -1  1  5 -2 -2  0 -1 -1  0 -4 
W -3 -3 -4 -4 -2 -2 -3 -2 -2 -3 -2 -3 -1  1 -4 -3 -2 11  2 -3 -4 -3 -2 -4 
Y -2 -2 -2 -3 -2 -1 -2 -3  2 -1 -1 -2 -1  3 -3 -2 -2  2  7 -1 -3 -2 -1 -4 
V  0 -3 -3 -3 -1 -2 -2 -3 -3  3  1 -2  1 -1 -2 -2  0 -3 -1  4 -3 -2 -1 -4 
B -2 -1  3  4 -3  0  1 -1  0 -3 -4  0 -3 -3 -2  0 -1 -4 -3 -3  4  1 -1 -4 
Z -1  0  0  1 -3  3  4 -2  0 -3 -3  1 -1 -3 -1  0 -1 -3 -2 -2  1  4 -1 -4 
X  0 -1 -1 -1 -2 -1 -1 -1 -1 -1 -1 -1 -1 -1 -2  0  0 -2 -1 -1 -1 -1 -1 -4 
* -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4 -4  1"


type Blosum = { aaLookup : string [] ; matrix : int [,] }

#if !FABLE_COMPILER
let loadBlosum () =
    let f = new StringReader(b62) // new StreamReader(@"C:\proj\SGD\BLOSUM62.txt")
    let rec getHeader() =
        let l =  f.ReadLine()
        if l.StartsWith("#") then getHeader() else l

    let header = getHeader()    
    let aaLookup = header.Split([|' '|]) |> Array.filter (fun x-> x <> "")

    let subMat = Array2D.create aaLookup.Length aaLookup.Length 0

    for j in { 0 .. aaLookup.Length-1 } do
        let cols = f.ReadLine().Split([|' '|]) |> Array.filter (fun x-> x <> "")
        let vals = Array.map (int) (cols.[1..])
        for i in { 0 .. aaLookup.Length-1 } do
            subMat.[j,i] <- vals.[i]
    { aaLookup = aaLookup ; matrix = subMat     }

let blosum = loadBlosum()        
    
let lookupBlosum blosum frAA toAA =
    let frAA_check, toAA_check = 
        match frAA, toAA with
        | '?', '?' -> '*', '*'
        | '?', _ -> '*', toAA
        | _, '?' -> frAA, '*'
        | _,_ -> frAA, toAA 
    let fi = Array.findIndex (fun a -> a = string(frAA_check)) blosum.aaLookup
    let ti = Array.findIndex (fun a -> a = string(toAA_check)) blosum.aaLookup            
    blosum.matrix.[fi,ti]
#endif

/// Efficient fasta sequence cleanup that removes newlines and spaces
let stripNewlines(s:string) =
    let rec count i t =
        if i = -1 then t
        elif s.[i] = '\n' || s.[i] = '\r' || s.[i] = ' ' then count(i-1) (t+1)
        else count (i-1) t

    let bad = count (s.Length-1) 0
    let needed = s.Length - bad
    let r = Array.create needed '#'
    let rec filter i j =
        if i = s.Length then j
        elif s.[i] = '\n' || s.[i] = '\r' || s.[i] = ' ' then filter (i+1) j
        else
            r.[j] <- s.[i]
            filter (i+1) (j+1)
    
    let finalJ = filter 0 0
    assert(finalJ = needed) // Should fill up target array exactly
    r

/// bulk load a fasta string (containing \n) into a hashtable 
let readReferenceFromString (fastaString:string) = 
    let fastaString2 = if fastaString.StartsWith(">") then fastaString.[1..] else fastaString
    let chrs = fastaString2.Replace("\r", "").Split([|"\n>"|],StringSplitOptions.RemoveEmptyEntries) 
    let splitEntry (s:string) =
        let firstEOL = s.IndexOf '\n'
        let header = s.Substring(0,firstEOL)
        let firstName = header.Split([|' '|]).[0]
        //let seq = s.Substring(firstEOL).ToCharArray() |> Array.filter (fun x -> ((x <> '\n') && (x <> '\r')))
        let seqAlt = s.Substring(firstEOL) |> stripNewlines
        //assert(seqAlt = seq)
        (firstName,seqAlt)

    let chrSeq = new Dictionary<string,char []>()

    chrs
    |> Seq.map (splitEntry)
    |> Seq.iter (fun (name,seq) -> 
        if chrSeq.ContainsKey(name) then failwith "XXX" // f "ERROR: duplicate fasta name '%s'" name
        else chrSeq.Add(name,seq))
    chrSeq


#if !FABLE_COMPILER
/// bulk load a fasta file into a hashtable    
let readReference (path:string) =
    //printf "readReference %d bytes\n" path.Length
    use f = new StreamReader(path)
    let fastaString = f.ReadToEnd()
    f.Close()
    readReferenceFromString fastaString

#endif
(*
let testDNA = "ATGCTACCTTTATATCTTTTAACAAATGCGAAGGGACAACAAATGCAAATAGAATTGAAAAACGGTGAAATTATACAAGGGATATTGACCAACGTAGATAACTGGATGAA" +
                "CCTTACTTTATCTAATGTAACCGAATATAGTGA" + 
                "AGAAAGCGCAATTAATTCAGAAGACAATGCTGAGAGCAGTAAAGCCGTAAAATTGAACGAAATTTATATTAGAGGGACTTTTATCAAGTTTATCAAATTGCAAGATA" +
                "ATATAATTGACAAGGTCAAGCAGCAAATTAACTCCAACAATAACTCTAATAGTAACGGCCCTGGGCATAAAAGATACTACAACAATAGGGATTCAAACAACAATAGAGGTA" +
                "ACTACAACAGAAGAAATAATAATAACGGCAACAGCAACCGCCGTCCATACTCTCAAAACCGTCAATACAACAACAGCAACAGCAGTAACATTAACAACAGTATCAACAGTATCA" +
                "ATAGCAACAACCAAAATATGAACAATGGTTTAGGTGGGTCCGTCCAACATCATTTTAACAGCTCTTCTCCACAAAAGGTCGAATTTTAAACAAATTTTGTATTATAATAATT" +
                "ATGTACATATATAAATATATTGGTACATATGTACTGTGTGTGTATGTGAATGTTGATTACCGTTTTCTTTAAAAAAAGCTTTTCTTCTTTTTATTACCGAGCTTTCCTTTAA"

            
let testprot =  "MLPLYLLTNAKGQQMQIELKNGEIIQGILTNVDNWMNLTLSNVTEYSEESAINSEDNAESSKAVKLNEIYIRGTFIKFIKLQDNIIDKVKQQINSNNNSNSN" +
                "GPGHKRYYNNRDSNNNRGNYNRRNNNNGNSNRRPYSQNRQYNNSNSSNINNSINSINSNNQNMNNGLGGSVQHHFNSSSPQKVEF*TNFVL**LCTYINI" +
                "LVHMYCVCM*MLITVFFKKSFSSFYYRAFL*"

printfn "DNA =%d %s\n" testDNA.Length testDNA
printfn "p1  =%s\n" testprot
printfn "xl  =%s\n" (translate (testDNA.ToCharArray()) |> arr2seq )
assert (testprot = (testDNA.ToCharArray() |> arr2seq ) )
*)
let fastaLineLength = 60
#if !FABLE_COMPILER
let rec _wrap offset (a : char []) (res:ResizeArray<char>) =
    if a.Length <= (offset+fastaLineLength) then
        res.AddRange(a.[offset..]) // breaks in rust
        res.Add('\n')
        res.ToArray() |> arr2seq
    else
        res.AddRange(a.[offset..(offset + fastaLineLength-1)] )
        res.Add('\n')
        _wrap (offset+fastaLineLength) a res

let wrap (a : char[]) =
    _wrap 0 a (new ResizeArray<char>())
#endif
#if !FABLE_COMPILER
(*

let dumpReference (genome:Map<string,char []>) (file:string) =
    use outF = new StreamWriter(file)
    for pk in genome do
        outF.Write(sprintf ">%s\n" pk.Key)
        outF.Write(wrap pk.Value)

let dumpReferenceToString (genome:Map<string,char []>) = 
    let sb = StringBuilder()
    for pk in genome do
        sb.Append(sprintf ">%s\n" pk.Key) |> ignore
        sb.Append((wrap pk.Value)) |> ignore
    sb.ToString()

*)
#endif
#if !FABLE_COMPILER
/// Produce a sequence of name,DNA string given a fasta path
let fastaStream (path:string) =
    seq {
        use inf = new StreamReader(path)
        let sb = new StringBuilder()
        let header = ref None
        while not (inf.EndOfStream) do
            let line = inf.ReadLine().Trim([|'\n' ; '\r' ; ' '|])
            if line.StartsWith(">") then
                match !header with
                | None -> 
                    header := Some(line.[1..])
                | Some(h) ->
                    yield h,sb.ToString()
                    sb.Clear() |> ignore
                    header := Some(line.[1..])
            else
                sb.Append(line) |> ignore
        match !header with | None -> () | Some(h) -> yield h,sb.ToString()
                
    }
#endif

/// coding regions extracted from a sequence
type CodingRegion =
    {fwd:bool; l:int; r:int; mRNA:char[]; aa:char []; hasStopAtEnd:bool}

    // get the underlying coding region starting at the first methionine
    member x.tryTrimToFirstMethionine() = 
        let firstMOp = x.aa |> Array.tryFindIndex(fun c -> c='M')
        match firstMOp with
        | None -> None
        | Some firstM ->
            Some {x with
                    l = (if x.fwd then (x.l+firstM*3) else x.l);
                    r = (if x.fwd then x.r else (x.r-firstM*3)); 
                    mRNA = x.mRNA.[firstM*3..]; 
                    aa = x.aa.[firstM..]}

/// filter ORFs from a methionine to a stop, ordered by decreasing protein length
let getProperOrfs (cdss:CodingRegion list) = 
    cdss
    |> List.filter (fun cds -> cds.hasStopAtEnd)
    |> List.choose (fun cds -> cds.tryTrimToFirstMethionine())
    |> List.sortBy (fun cds -> -cds.aa.Length)

/// Finds coding regions in the six frames of the provided sequence, ordered by decreasing protein length
/// Does not leave the stop codon at the end of the mRNA/AA seq. Does not trim down to the first methionine. 
/// Use the tryTrimToFirstMethionine method of CodingRegion to do that.
let findCodingRegions(inputSeq:char []) =
    [for phase in [0;1;2] do
        for fwd in [true;false] do
            let mRNA =
                if fwd then inputSeq.[phase..]
                else revComp inputSeq.[..inputSeq.Length-1-phase]

            let aa = translate mRNA |> arr2seq

            let rec getCodingRegions (codingRegions:CodingRegion list) (currRnaPos:int) =
                let currAa = aa.[currRnaPos/3..]
                let firstStop = currAa.IndexOf('*')*3
                if firstStop < 0 then
                    let l,r =
                        if fwd then currRnaPos + phase, inputSeq.Length-1
                        else 0, inputSeq.Length-1 - (currRnaPos + phase)

                    {fwd = fwd;
                        l = l;
                        r = r;
                        mRNA = mRNA.[currRnaPos..];
                        aa = currAa.ToCharArray();
                        hasStopAtEnd = false}
                    ::codingRegions
                else
                    let l, r =
                        if fwd then currRnaPos + phase, currRnaPos + phase + firstStop - 1
                        else inputSeq.Length-1 - (currRnaPos + phase + firstStop - 1), inputSeq.Length-1 - (currRnaPos + phase)
                    let newCodingRegions =
                        {fwd = fwd;
                            l = l;
                            r = r;
                            mRNA = mRNA.[currRnaPos..(currRnaPos+firstStop-1)];
                            aa = currAa.[..firstStop/3-1].ToCharArray();
                            hasStopAtEnd= true}
                        ::codingRegions
                    if currAa.Length > (firstStop/3+1) then
                        getCodingRegions newCodingRegions (currRnaPos + firstStop + 3) 
                    else
                        newCodingRegions

            yield getCodingRegions [] 0
    ] 
    |> List.concat
    |> List.sortBy (fun cds -> -cds.aa.Length)