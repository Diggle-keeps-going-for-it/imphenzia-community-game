#! python

from pathlib import Path


class PathError(Exception):
    def __init__(self, message : str):
        self.message = message


    def __str__(self) -> str:
        return self.message


def check_path_exists_as_directory(path : Path, argument_name : str) -> None:
    if not path.exists():
        raise PathError(f'{argument_name} path "{path}" does not exist')
    if not path.is_dir():
        raise PathError(f'{argument_name} path "{path}" is not a directory')
