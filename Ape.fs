open System

type Syntax =
    | Word of string
    | Quote of Syntax list

(*
Evaluation is a recursive state->state function where the state is a dictionary (let bindings), a data stack (immutable
list), and a program list.

The form v [q] cons is rewritten to [v q].
The form [v q] snoc is rewritten to v [q].
The form x y t f eq is rewritten to t or f depending on whether x = y (quotations expanded).
The form x n let adds a dictionary entry binding n to x.

The form w is treated as a word or a literal.
If w is found in the dictionary as a Word then it is prepended to the program for execution.
If w is found in the dictionary as a Quote then it is unpacked and concatenated to the program for execution.
Otherwise, w is treated as a literal and placed on the stack.
*)

let eval dict stack code =
    let rec eval' state =
        match state with
        | dict, Quote q :: v :: stack', Word "cons" :: code' -> eval' (dict, Quote (v :: q) :: stack', code')
        | dict, Quote (v :: q) :: stack', Word "snoc" :: code' -> eval' (dict, Quote q :: v :: stack', code')
        | dict, f :: t :: x :: y :: stack, Word "eq" :: code' ->
            match if x = y then t else f with
            | Word w -> eval' (dict, stack, Word w :: code')
            | Quote q -> eval' (dict, stack, q @ code')
        | dict, v :: stack', n :: Word "let" :: code' -> eval' (Map.add n v dict, stack', code')
        | dict, stack, word :: code' ->
            match Map.tryFind word dict with
            | Some (Word found) -> eval' (dict, stack, Word found :: code')
            | Some (Quote found) -> eval' (dict, stack, found @ code')
            | None -> eval' (dict, word :: stack, code')
        | state -> state
    eval' (dict, stack, code)

(* Lexer, parser, pretty printer *)

let lex source =
    let rec lex' tok tokens = function
        | '[' :: t -> lex' [] (['['] :: tok :: tokens) t
        | ']' :: t -> lex' [] ([']'] :: tok :: tokens) t
        | s :: t when Char.IsWhiteSpace s -> lex' [] (tok :: tokens) t
        | c :: t -> lex' (c :: tok) tokens t
        | [] -> tok :: tokens |> List.filter ((<>) []) |> List.rev |> List.map (List.fold (fun s t -> string t + s) "")
    source |> List.ofSeq |> lex' [] []

let parse tokens =
    let rec parse' result = function
        | "[" :: t -> let r, t' = parse' [] t in parse' (Quote r :: result) t'
        | "]" :: t -> List.rev result, t
        | w :: t-> parse' (Word w :: result) t
        | [] -> List.rev result, []
    tokens |> parse' [] |> fst

let print code =
    let rec print' output = function
        | Word s :: t -> print' (sprintf "%s%s " output s) t
        | Quote q :: t -> print' (sprintf "%s[%s] " output (print' "" q)) t
        | [] -> let len = output.Length in if len = 0 then "" else output.Substring(0, len - 1)
    print' "" code

(* Initialize dictionary with some useful things... *)

let prelude = "
[_ let]                       drop   let
[a let a]                     apply  let
[[] cons]                     quote  let
[quote a let a a]             dup    let
[a let quote b let a b]       dip    let
[quote a let quote e let a e] swap   let
[snoc drop]                   head   let
[snoc swap drop]              tail   let
[[#t swap] dip eq]            if     let
[#f #t if]                    not?   let
[[] [drop #f] if]             and?   let
[[drop #t] [#t #t #f eq] if]  or?    let
[#t [not?] [] eq]             xor?   let
[#t #f eq]                    equal? let
[[] equal?]                   empty? let
[0 equal?]                    zero?  let
[]                            true?  let
[not?]                        false? let
"

let dictionary, _, _ = prelude |> lex |> parse |> eval Map.empty []

(* Tests *)

let test code expected =
    let _, stack, _ = code |> lex |> parse |> eval dictionary []
    let result = List.rev stack |> print
    if result <> expected then printfn "FAIL: %A <> %A" result expected

test "a [] cons"                    "[a]"
test "a [b c] cons"                 "[a b c]"
test "[a] [b c] cons"               "[[a] b c]"
test "[a] snoc"                     "a []"
test "[a b c] snoc"                 "a [b c]"
test "[[a] b c] snoc"               "[a] [b c]"
test "foo foo yes no eq"            "yes"
test "foo bar yes no eq"            "no"
test "foo foo [yes] [no] eq"        "yes"
test "foo bar [yes] [no] eq"        "no"
test "[x y [z]] [x y [z]] y n eq"   "y"
test "2.71 e let e"                 "2.71"
test "[cons cons] cc let a b [] cc" "[a b]"
test "123 456 drop"                 "123"
test "123 456 [drop] apply"         "123"
test "123 quote"                    "[123]"
test "123 dup"                      "123 123"
test "123 456 [quote] dip"          "[123] 456"
test "123 456 swap"                 "456 123"
test "[a b c] head"                 "a"
test "[a b c] tail"                 "[b c]"
test "#t 123 456 if"                "123"
test "#f 123 456 if"                "456"
test "#t not?"                      "#f"
test "#f not?"                      "#t"
test "#t #t and?"                   "#t"
test "#f #t and?"                   "#f"
test "#t #f and?"                   "#f"
test "#f #f and?"                   "#f"
test "#t #t or?"                    "#t"
test "#f #t or?"                    "#t"
test "#t #f or?"                    "#t"
test "#f #f or?"                    "#f"
test "#t #t xor?"                   "#f"
test "#f #t xor?"                   "#t"
test "#t #f xor?"                   "#t"
test "#f #f xor?"                   "#f"
test "foo foo equal?"               "#t"
test "foo bar equal?"               "#f"
test "[] empty?"                    "#t"
test "[foo] empty?"                 "#f"
test "0 zero?"                      "#t"
test "42 zero?"                     "#f"
test "#t true?"                     "#t"
test "#f true?"                     "#f"
test "#t false?"                    "#f"
test "#f false?"                    "#t"

(* REPL *)

let rec repl (dict, stack, code) () =
    try
        let (dict, stack, _) as state' = Console.ReadLine() |> lex |> parse |> eval dict stack
        List.rev stack |> print |> printf "%s\n>" |> repl state'
    with ex -> printf "ERROR: %s" ex.Message |> repl (dict, stack, code)

printf "Welcome to Ape\n>"
repl (dictionary, [], []) ()
