# Grammar

## ABNF Grammar Specification

```abnf
; Root Rule: A script is a sequence of statements
script              = statement *( ";" statement ) [ ";" ]

; Statements
statement           = var-declaration
                    / assignment
                    / expression
                    / block
                    / loop
                    / try-catch

var-declaration     = "var" identifier "=" expression

assignment          = identifier assignment-operator expression
assignment-operator = "=" / "+=" / "-=" / "*=" / "/="

expression          = literal
                    / identifier
                    / unary-expression
                    / binary-expression
                    / postfix-expression
                    / lambda
                    / method-call
                    / object-instantiation
                    / conditional
                    / switch-expression
                    / block

block               = "{" *( statement ";" ) [ statement ] "}"

; Literals
literal             = integer-literal / float-literal / double-literal
                    / long-literal / short-literal / decimal-literal
                    / string-literal / "null" / "true" / "false"

integer-literal     = DIGIT1 *( DIGIT ) ["n"]
float-literal       = DIGIT1 *( DIGIT ) "." *( DIGIT ) "f"
double-literal      = DIGIT1 *( DIGIT ) "." *( DIGIT ) "d"
long-literal        = DIGIT1 *( DIGIT ) "l"
short-literal       = DIGIT1 *( DIGIT ) "s"
decimal-literal     = DIGIT1 *( DIGIT ) "." *( DIGIT ) "m"
string-literal      = DQUOTE *( %x20-21 / %x23-7E ) DQUOTE

; Unary and Postfix Expressions
unary-expression    = operator expression
operator            = "-" / "!"

postfix-expression  = identifier ( "++" / "--" )

; Binary Expressions
binary-expression   = expression operator expression
operator            = "+" / "-" / "*" / "/" / "%"
                    / "==" / "!=" / ">" / "<" / ">=" / "<="
                    / "??"
                    / "&&" / "||"

; Conditionals
conditional         = "if" "(" expression ")" block [ "else" block ]

; Switch Expression
switch-expression   = "switch" "(" expression ")" "{" case-list "}"
case-list           = *( case-statement )
case-statement      = "case" expression ":" expression ";"
                    / "default" ":" expression ";"

; Loops
loop                = "loop" block
break-statement     = "break"
continue-statement  = "continue"

; Try-Catch
try-catch           = "try" block catch-block
catch-block         = "catch" "(" typename identifier ")" block

; Object Instantiation
object-instantiation = "new" typename "(" [ argument-list ] ")"

; Lambdas
lambda              = "(" [ parameter-list ] ")" "=>" expression
parameter-list      = identifier *( "," identifier )

; Method Calls
method-call         = identifier "." identifier "(" [ argument-list ] ")"
                    / method-call "." identifier "(" [ argument-list ] ")"

argument-list       = expression *( "," expression )

; Identifiers and Typenames
identifier          = ALPHA *( ALPHA / DIGIT / "_" )
typename            = identifier *( "." identifier ) [ generic-args ]
generic-args        = "<" typename *( "," typename ) ">"

; Misc
DIGIT               = %x30-39
DIGIT1              = %x31-39
ALPHA               = %x41-5A / %x61-7A
DQUOTE              = %x22
```
