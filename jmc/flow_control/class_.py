import regex
import re
from typing import TYPE_CHECKING, List

from ..utils import BracketRegex
from .. import Logger

if TYPE_CHECKING:
    from ..datapack import DataPack
    from ..command import Command

logger = Logger(__name__)

bracket_regex = BracketRegex()
class_REGEX = r'^class\s*([\w\._]+)\s*' + bracket_regex.match_bracket('{}', 2)  # noqa


def process_class(self: "DataPack", line: str, prefix=''):
    logger.debug(f"Searching for class (prefix = {prefix})")

    line = line.strip()
    class_name: str

    def class_found(match: re.Match):
        nonlocal class_name
        class_name, class_content = bracket_regex.compile(match.groups())
        logger.debug(f"Class found - {prefix}{class_name}")
        remaining_line = self.process_function(
            class_content, prefix=f'{prefix}{class_name}.')
        return remaining_line
    line, success = regex.subn(class_REGEX, class_found, line)

    if success:
        logger.debug(f"Recursing process_class()")
        line = self.process_class(line, prefix)

    else:
        logger.debug(f"No Class found")
    return line