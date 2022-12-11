#!/bin/sh

# docker pull ghcr.io/calico-crusade/cardboardbox-anime/ui:latest
docker pull ghcr.io/calico-crusade/cardboardbox-anime/api:latest
docker pull ghcr.io/calico-crusade/cardboardbox-anime/bot:latest
docker pull ghcr.io/calico-crusade/cardboardbox-anime/background:latest
docker pull ghcr.io/calico-crusade/cardboardbox-anime/web:latest
docker pull ghcr.io/calico-crusade/cardboardbox-proxy/api:latest

docker-compose -f docker-compose.yml up -d