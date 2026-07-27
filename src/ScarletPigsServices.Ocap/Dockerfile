FROM ghcr.io/ocap2/web:2.1.1@sha256:7eac2c20767d024351ac387ecb976318c80b8befb0d0758fefbc7ba025cef9be

COPY volume/ /opt/scarletpigs-ocap-volume/
COPY --chmod=755 docker-entrypoint.sh /scarletpigs-entrypoint.sh

ENTRYPOINT ["/scarletpigs-entrypoint.sh"]
