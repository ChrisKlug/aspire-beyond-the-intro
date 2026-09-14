#!/usr/bin/env bash
ps -eww -o pid=,args= | awk -v pat="dcp" '
$0 ~ pat && $2 !~ /awk$/ {
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