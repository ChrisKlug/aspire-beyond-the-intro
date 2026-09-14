#!/usr/bin/env bash
# procpid PID   - show one process, one row per argument
ps -ww -o pid=,args= -p "$1" | awk '
{
  pid = $1
  sub(".*/", "", $2); name = $2
  first = 1
  for (i = 3; i <= NF; i++) {
    arg = $i
    if (arg ~ /^-/ && i < NF && $(i+1) !~ /^-/) { arg = arg " " $(i+1); i++ }
    printf "%-6s %-12s %s\n", (first ? pid : ""), (first ? name : ""), arg
    first = 0
  }
  print "---"
}'