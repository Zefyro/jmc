import unittest
import sys


sys.path.append('./src')  # noqa

from tests.utils import string_to_tree_dict
from jmc.test_compile import JMCPack


class TestFunction(unittest.TestCase):
    def test_define(self): ...
    def test_call(self): ...
    def test_anonymous(self): ...
    def test_class(self): ...


class TestFeatures(unittest.TestCase):
    def test_import(self): ...
    def test_comment(self): ...
    def test_tick(self): ...


if __name__ == '__main__':
    unittest.main()
