from dataclasses import dataclass, field
from ast import literal_eval
from pprint import pprint
from typing import Optional
from enum import Enum
import re

from .exception import JMCSyntaxException
from .log import Logger


logger = Logger(__name__)


class TokenType(Enum):
    keyword = "Keyword"
    paren = "Parentheses"
    paren_round = "RoundParentheses"
    paren_square = "SquareParentheses"
    paren_curly = "CurlyParentheses"
    string = "StringLiteral"
    comment = "Comment"
    comma = "Comma"
    func = "Function"


@dataclass(frozen=True, eq=False)
class Token:
    token_type: TokenType
    line: int
    col: int
    string: str
    length: int = field(init=False, repr=False)

    def __post_init__(self):
        object.__setattr__(self, "length", len(
            self.string)+2 if self.token_type == TokenType.string else len(self.string))


@dataclass(frozen=True, eq=False)
class Pos:
    line: int
    col: int


class Re:
    NEW_LINE = '\n'
    BACKSLASH = '\\'
    WHITESPACE = r'\s+'
    KEYWORD = r'[a-zA-Z0-9_\.\/\^\~]'
    SEMICOLON = ';'
    COMMA = ","
    HASH = '#'
    SLASH = "/"


class Quote:
    SINGLE = "'"
    DOUBLE = '"'


class Paren:
    L_ROUND = '('
    R_ROUND = ')'
    L_SQUARE = '['
    R_SQUARE = ']'
    L_CURLY = '{'
    R_CURLY = '}'


PAREN_PAIR = {
    Paren.L_CURLY: Paren.R_CURLY,
    Paren.L_SQUARE: Paren.R_SQUARE,
    Paren.L_ROUND: Paren.R_ROUND,
}


class Tokenizer:
    line: int
    col: int

    state: Optional[TokenType]
    token: str
    token_pos: Optional[Pos]
    keywords: list[Token]

    list_of_tokens: list[list[Token]]

    # String
    quote: Optional[str]
    is_escaped: bool
    """Is it an escaped character (Backslashed)"""

    # Parenthesis
    paren: Optional[str]
    r_paren: Optional[str]
    paren_count: Optional[int]
    is_string: bool

    # Comment
    is_slash: bool

    def __init__(self, raw_string: str, file_path_str: str, line: int = 1, col: int = 1, file_string: str = None) -> None:
        logger.debug("Initializing Tokenizer")
        self.raw_string = raw_string
        if file_string is None:
            self.file_string = raw_string
        else:
            self.file_string = file_string
        self.file_path = file_path_str
        self.programs = self.parse(self.raw_string, line=line, col=col)

    def append_token(self) -> None:
        self.keywords.append(
            Token(self.state,
                  self.token_pos.line,
                  self.token_pos.col,
                  self.token)
        )
        self.token = ""
        self.token_pos = None
        self.state = None

    def append_keywords(self) -> None:
        if len(self.keywords) != 0:
            logger.debug(f"Appending keywords: {self.keywords}")
            self.list_of_tokens.append(self.keywords)
            self.keywords = []

    def parse(self, string: str, line: int, col: int, expect_semicolon=True) -> list[list[Token]]:
        self.list_of_tokens = []
        self.line = line
        self.col = col - 1
        self.keywords = []
        self.state = None
        self.token = ""
        self.token_pos = None
        # String
        self.quote = None
        self.is_escaped = False
        # Parenthesis
        self.paren = None
        self.r_paren = None
        self.paren_count = None
        self.is_string = False
        # Comment
        self.is_slash = False

        for char in string:
            self.col += 1
            if not expect_semicolon and char == Re.SEMICOLON:
                raise JMCSyntaxException(
                    f"In {self.file_path}\nUnexpected semicolon(;) at line {self.line} col {self.col}.\n{self.raw_string.split(Re.NEW_LINE)[self.line-1][:self.col]} <-")

            if char == Re.NEW_LINE:
                if self.state == TokenType.string:
                    raise JMCSyntaxException(
                        f"In {self.file_path}\nString literal at line {self.line} contains an unescaped line break.\n{self.raw_string.split(Re.NEW_LINE)[self.line-1]} <-")
                elif self.state == TokenType.comment:
                    self.state = None
                elif self.state == TokenType.keyword:
                    self.append_token()
                elif self.state == TokenType.paren:
                    self.token += char
                self.line += 1
                self.col = 0
                continue

            if char == Re.SLASH and self.is_slash and self.state != TokenType.string:
                self.state = TokenType.comment
                self.token = self.token[:-1]
                continue

            if self.state == TokenType.keyword:
                if char in [
                    Quote.SINGLE,
                    Quote.DOUBLE,
                    Paren.L_CURLY,
                    Paren.L_ROUND,
                    Paren.L_SQUARE,
                    Re.SEMICOLON,
                    Re.COMMA
                ] or re.match(Re.WHITESPACE, char):
                    self.append_token()
                else:
                    self.token += char
                    continue

            if self.state == None:
                if char in [Quote.SINGLE, Quote.DOUBLE]:
                    self.state = TokenType.string
                    self.token_pos = Pos(self.line, self.col)
                    self.quote = char
                    self.token += char
                elif re.match(Re.WHITESPACE, char):
                    continue
                elif char == Re.SEMICOLON:
                    self.append_keywords()
                elif char in [Paren.L_CURLY, Paren.L_ROUND, Paren.L_SQUARE]:
                    self.state = TokenType.paren
                    self.token += char
                    self.token_pos = Pos(self.line, self.col)
                    self.paren = char
                    self.r_paren = PAREN_PAIR[char]
                    self.paren_count = 0
                elif char in [Paren.R_CURLY, Paren.R_ROUND, Paren.R_SQUARE]:
                    raise JMCSyntaxException(
                        f"In {self.file_path}\nUnexpected bracket at line {self.line} col {self.col}.\n{self.raw_string.split(Re.NEW_LINE)[self.line-1][:self.col]} <-")
                elif char == Re.HASH and self.col == 1:
                    self.state = TokenType.comment
                elif char == Re.COMMA:
                    self.token += char
                    self.token_pos = Pos(self.line, self.col)
                    self.state = TokenType.comma
                    self.append_token()
                else:
                    self.state = TokenType.keyword
                    self.token_pos = Pos(self.line, self.col)
                    self.token += char

            elif self.state == TokenType.string:
                self.token += char
                if char == Re.BACKSLASH and not self.is_escaped:
                    self.is_escaped = True
                elif char == self.quote and not self.is_escaped:
                    self.token = literal_eval(self.token)
                    self.append_token()
                elif self.is_escaped:
                    self.is_escaped = False

            elif self.state == TokenType.paren:
                self.token += char
                if self.is_string:
                    if char == Re.BACKSLASH and not self.is_escaped:
                        self.is_escaped = True
                    elif char == self.quote and not self.is_escaped:
                        self.is_string = False
                    elif self.is_escaped:
                        self.is_escaped = False
                else:
                    if char == self.r_paren and self.paren_count == 0:
                        is_end = False
                        if self.paren == Paren.L_CURLY:
                            self.state = TokenType.paren_curly
                            is_end = True
                        elif self.paren == Paren.L_ROUND:
                            self.state = TokenType.paren_round
                        elif self.paren == Paren.L_SQUARE:
                            self.state = TokenType.paren_square
                        self.append_token()
                        if is_end and expect_semicolon:
                            self.append_keywords()
                        continue

                    if char == self.paren:
                        self.paren_count += 1
                    elif char == self.r_paren:
                        self.paren_count -= 1
                    elif char in [Quote.SINGLE, Quote.DOUBLE]:
                        self.is_string = True
                        self.quote = char

            elif self.state == TokenType.comment:
                pass

            self.is_slash = (char == Re.SLASH)

        if self.state == TokenType.keyword:
            if self.token != "":
                self.append_token()
            if not expect_semicolon:
                self.append_keywords()

        if self.state == TokenType.string:
            raise JMCSyntaxException(
                f"In {self.file_path}\nString literal at line {self.line} contains an unescaped line break.\n{self.raw_string.split(Re.NEW_LINE)[self.line-1]} <-")
        elif self.state == TokenType.paren:
            raise JMCSyntaxException(
                f"In {self.file_path}\nBracket at line {self.token_pos.line} col {self.token_pos.col} was never closed.\n{self.raw_string.split(Re.NEW_LINE)[self.token_pos.line-1][:self.token_pos.col]} <-")
        elif len(self.keywords) != 0:
            raise JMCSyntaxException(
                f"In {self.file_path}\nExpected semicolon(;) at line {self.keywords[-1].line} col {self.keywords[-1].col+self.keywords[-1].length}.\n{self.raw_string.split(Re.NEW_LINE)[self.keywords[-1].line-1][:self.keywords[-1].col+self.keywords[-1].length]} <-")

        if expect_semicolon:
            return self.list_of_tokens
        else:
            return self.list_of_tokens[0]

    def parse_func_args(self, token: Token) -> tuple[list[Token], dict[str, Token]]:
        if token.token_type != TokenType.paren_round:
            raise JMCSyntaxException(
                f"In {self.file_path}\nExpected ( at line {token.line} col {token.col}.\n{self.raw_string.split(Re.NEW_LINE)[self.line-1]} <-"
            )
        keywords = self.parse(
            token.string[1:-1], line=token.line, col=token.col, expect_semicolon=False)
        args: list[Token] = []
        kwargs: dict[str, Token] = dict()
        key: str = ""
        arg: str = ""
        arrow_func_state = 0
        """
        0: None
        1: ()
        2: =>
        """

        def add_arg(token: Token) -> None:
            nonlocal arg
            nonlocal args
            if kwargs:
                raise JMCSyntaxException(
                    f"In {self.file_path}\nPositional argument follows keyword argument at line {token.line} col {token.col+1}.\n{self.raw_string.split(Re.NEW_LINE)[token.line-1][:token.col+1]} <-"
                )
            args.append(Token(string=arg, line=token.line,
                              col=token.col, token_type=token.token_type))
            arg = ""

        def add_key(token: Token) -> None:
            nonlocal key
            nonlocal arg
            nonlocal kwargs
            if key[0] in [Paren.L_CURLY, Paren.L_ROUND, Paren.L_SQUARE]:
                raise JMCSyntaxException(
                    f"In {self.file_path}\nInvalid key({key}) at line {last_token.line} col {last_token.col}.\n{self.raw_string.split(Re.NEW_LINE)[last_token.line-1][:last_token.col]} <-"
                )
            if key == "":
                raise JMCSyntaxException(
                    f"In {self.file_path}\nEmpty at line {token.line} col {token.col}.\n{self.raw_string.split(Re.NEW_LINE)[token.line-1][:token.col]} <-"
                )

            if key in kwargs:
                raise JMCSyntaxException(
                    f"In {self.file_path}\nDuplicated key({key}) at line {token.line} col {token.col}.\n{self.raw_string.split(Re.NEW_LINE)[token.line-1][:token.col]} <-"
                )
            kwargs[key] = Token(string=arg, line=token.line,
                                col=token.col, token_type=token.token_type)
            key = ""
            arg = ""

        for token in keywords:
            # print(token.string, args, kwargs, arrow_func_state)
            if arrow_func_state > 0:
                if arrow_func_state == 1:
                    if token.string == "=>" and token.token_type == TokenType.keyword:
                        arrow_func_state = 2
                        last_token = token
                        continue
                    else:
                        arg = last_token.string
                        if key:
                            add_key(last_token)
                        arrow_func_state = 0
                        continue
                elif arrow_func_state == 2:
                    if token.token_type == TokenType.paren_curly:
                        new_token = Token(
                            string=token.string[1:-1], line=token.line, col=token.col, token_type=TokenType.func)
                        arg = new_token.string
                        if key:
                            add_key(new_token)
                        last_token = new_token
                        arrow_func_state = 0
                        continue
                    else:
                        raise JMCSyntaxException(
                            f"In {self.file_path}\nExpected {'{'} at line {token.line} col {token.col}.\n{self.raw_string.split(Re.NEW_LINE)[token.line-1][:token.col]} <-"
                        )

            if token.token_type == TokenType.keyword:
                if arg:
                    if token.string.startswith("="):
                        key = arg
                        arg = token.string[1:]
                        if arg:
                            add_key(token)
                    else:
                        raise JMCSyntaxException(
                            f"In {self.file_path}\nUnexpected token at line {token.line} col {token.col}.\n{self.raw_string.split(Re.NEW_LINE)[token.line-1][:token.col+token.length]} <-"
                        )
                elif key:
                    arg = token.string
                    if "=" in token.string:
                        col = token.col + token.string.find('=')
                        raise JMCSyntaxException(
                            f"In {self.file_path}\nDuplicated equal sign(=) at line {token.line} col {col+1}.\n{self.raw_string.split(Re.NEW_LINE)[token.line-1][:col+1]} <-"
                        )
                    add_key(token)
                else:
                    equal_sign_count = token.string.count('=')
                    if equal_sign_count > 1:
                        col = token.col + token.string.rfind('=') + 1
                        raise JMCSyntaxException(
                            f"In {self.file_path}\nDuplicated equal sign(=) at line {token.line} col {col}.\n{self.raw_string.split(Re.NEW_LINE)[token.line-1][:col]} <-"
                        )
                    if token.string.endswith("="):
                        key = token.string[:-1]
                    elif "=" in token.string:
                        key, arg = token.string.split('=')
                        add_key(token)
                    else:
                        arg = token.string

            elif token.token_type == TokenType.comma:
                arrow_func_state = 0
                if arg:
                    add_arg(last_token)
            elif token.token_type in [TokenType.paren_round, TokenType.paren_curly, TokenType.paren_square]:
                if token.string == "()":
                    arrow_func_state = 1
                else:
                    arg = token.string
                    if key:
                        add_key(token)

            elif token.token_type == TokenType.string:
                arg = token.string
                if key:
                    add_key(token)
            last_token = token

        if arg:
            add_arg(token)
        pprint(args)
        pprint(kwargs)

        return args, kwargs
