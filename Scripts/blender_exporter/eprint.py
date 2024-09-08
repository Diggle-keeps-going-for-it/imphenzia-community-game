#! python

import sys


def eprint(*args, **kwargs) -> None:
    print(*args, file=sys.stderr, **kwargs)
