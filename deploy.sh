#!/bin/sh

docker pull ghcr.io/calico-crusade/cardboardbox-anime/ui:latest
docker pull ghcr.io/calico-crusade/cardboardbox-anime/api:latest

docker-compose -f docker-compose.yml up -d