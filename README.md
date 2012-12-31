# Ape #

There are but four operations: One for abstraction (let bindings), two for composition (cons/snoc), and one equality (eq) op. Inspired by Joy and Factor, there are “words” and there are “quotations”. The mechanics are made from two stacks and a dictionary. The first stack is for data, the second for program continuation. You can probably glean from the prelude and tests how things work... Have fun!

# Barebones #

What I really set out to do was to see what the minimal set of primitives should be. I found it annoying that Joy and Cat had all these stack manipulation primitives such as “dip”, “dup”, “pop”, “swap”, etc. My little moment of eureka was when I realized I could add a “let” primitive and get my scoped binding as well as be able to get rid of these other primitives!

The language and data structure revolves around an extremely simplified AST structure:

> type Syntax = Word of string | Quote of Syntax list

We have atomic Words and composite Quotations (of Words and/or other Quotes). That’s it! There is no native concept of Integers, Booleans, etc.  Only Symbols. Further, there are only four primitive operations!

## cons/snoc ##

We need a means of composing and decomposing structures. This is done through cons (adding a given node to a List) and snoc (breaking a given List into it’s “head” node and “tail” remaining List). For example:

> a []      cons yields [a]
> a [b c]   cons yields [a b c]
> [a] [b c] cons yields [[a] b c]

Decomposition yields the opposite:

> [a]       snoc yields a []
> [a b c]   snoc yields a [b c]
> [[a] b c] snoc yields [a] [b c]

## eq ##

For Symbols to be of any use at all, we need at least one operation we can perform on them. The single operation in the language is eq which compares two Symbols (or Lists) and evaluates one or another expression as a result. It takes four arguments, compares the first two and evaluates the third or fourth. For example:

> foo foo yes no eq yields yes
> foo bar yes no eq yields no

The equality comparison walks the complete structure in the case of Quotes. For example:

> [foo bar [baz]] [foo bar [baz]] [yes] [no] eq yields yes

## let ##

The final missing piece is a means of abstraction. We need to be able to introduce new “words” to the language and then use them as if they were primitives. For this, we have let.

Let can be used to define new primitives by assigning a name to a value. For example, we could define a new “triple cons” operation such as:

> [cons cons cons] tcons let

And now can, for example, use our new “word” to cons three Symbols onto an empty list:

> a b c [] tcons yields [a b c]

# Prelude #

Now back to the so-called primitives who’s existence I found annoying in Joy and Cat. These can now be implemented in terms of the core four (cons, uncons, eq, let):

> [x let]                             pop   let // throw away top stack node
> [a let a]                           apply let // evaluate top stack List
> [[] cons]                           quote let // wrap top stack node into a List
> [quote a let a a]                   dup   let // duplicate top stack node
> [quote a let quote b let a apply b] dip   let // apply 2nd stack node
> [quote a let quote e let a e]       swap  let // apply 2nd stack node

I like this much better than adding them to the core language!

You may have also noticed that the standard Lisp-esque car and cdr (or Haskell-esque head and tail) are missing from the language. They can be defined in terms of uncons:

> [snoc swap pop] head let
> [snoc pop]      tail let

## Boolean Logic ##

There is no Boolean type in the language. There is not even an if word! Let’s implement them!

Using the symbols #t and #f to mean True and False (in Scheme-esque fashion), we can implement if in terms of eq:

> [[#t swap] dip eq] if let

And we can start implementing the standard Boolean logic operators (not, and, or, xor) in terms of this:

> [#f #t if]                  not? let
> [[] [pop #f] if]            and? let
> [[pop #t] [#t #t #f eq] if] or?  let
> [#t [not?] [] eq]           xor? let

Also useful will be to create function that check equality to well known values and return #t or #f for use with other Boolean operators:

> [#t #f eq]  equal? let
> [[] equal?] empty? let
> [0 equal?]  zero?  let
> []          true?  let // wow, easy!
> [not?]      false? Let

# What now? #

Obviously we can add more primitive operations. We could see how far we can take it as is; perhaps adding Church numerals and such (something like http://blogs.msdn.com/b/ashleyf/archive/2008/12/03/the-lambda-calculus.aspx ).

I did, at one point, add Turtle Graphics primitives, which turned out pretty fun: http://blogs.msdn.com/b/ashleyf/archive/2009/04/30/turtle-soup.aspx

The mechanics of the interpreter could use some tweaks. One idea is to change the dictionary to a list of dictionaries and then add/remove frames as definitions are expanded and then have completed execution. This would give nested let bindings that go out of scope.
