import logging

# Containers should emit logs to their standard stream. Writing beside the
# application would fail when the image is run with a read-only/non-root /app.
log_handler = logging.StreamHandler()
