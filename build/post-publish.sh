#!/bin/sh
# Lets every user read the published files and run the ones their owner may run.
chmod -R a+rX "$BUILD_APP_BIN"
