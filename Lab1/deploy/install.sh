#!/usr/bin/env bash
# =============================================================================
# install.sh — Automatic deployment of mywebapp on Ubuntu 22.04 LTS
# Usage: sudo bash install.sh
# =============================================================================
set -euo pipefail

# ── Configurable variables ────────────────────────────────────────────────────
REPO_URL="https://github.com/besade/KPI-DEPLOY-2026"
APP_DIR="/opt/mywebapp"
SRC_DIR="/tmp/mywebapp_build"
ENV_FILE="/etc/mywebapp/env"
VARIANT_N=2
DB_NAME="mywebapp"
DB_USER="mywebapp"
DEFAULT_PASSWORD="12345678"

# ── Colours ───────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
error() { echo -e "${RED}[ERROR]${NC} $*" >&2; exit 1; }

[[ $EUID -eq 0 ]] || error "Run as root: sudo bash install.sh"

# =============================================================================
# 1. System packages
# =============================================================================
info "Updating package lists…"
apt-get update -qq

info "Installing dependencies…"
apt-get install -y -qq \
    curl wget git nginx mariadb-server \
    apt-transport-https ca-certificates gnupg

# .NET 8 SDK (for building) + runtime
if ! command -v dotnet &>/dev/null; then
    info "Installing .NET 8 SDK…"
    wget -qO /tmp/packages-microsoft-prod.deb \
        "https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb"
    dpkg -i /tmp/packages-microsoft-prod.deb
    apt-get update -qq
    apt-get install -y -qq dotnet-sdk-8.0
fi

# =============================================================================
# 2. Create system users
# =============================================================================
create_user() {
    local user=$1 comment=$2 groups=$3 shell=$4 system=$5
    if [[ $system == "yes" ]]; then
    useradd -r -s /usr/sbin/nologin -c "$comment" "$user"
	else
    # If group with same name already exists, use it
    if getent group "$user" >/dev/null; then
        useradd -m -g "$user" -s "$shell" -c "$comment" "$user"
    else
        useradd -m -U -s "$shell" -c "$comment" "$user"
    fi

    echo "$user:$DEFAULT_PASSWORD" | chpasswd
    passwd --expire "$user"
	fi
    else
        warn "User $user already exists, skipping."
    fi
    [[ -n $groups ]] && usermod -aG "$groups" "$user" || true
}

create_user "student"  "Student developer"       "sudo"    "/bin/bash" "no"
create_user "teacher"  "Teacher / grader"        "sudo"    "/bin/bash" "no"
create_user "operator" "Service operator"        ""        "/bin/bash" "no"
create_user "app"      "mywebapp service account" ""       "/bin/bash" "yes"

# Write N to variant
info "Writing variant…"
echo "$VARIANT_N" > /home/student/variant
chown student:student /home/student/variant

# ── sudoers for operator ──────────────────────────────────────────────────────
info "Configuring sudoers for operator…"
cat > /etc/sudoers.d/operator <<'EOF'
# operator can manage mywebapp service and reload nginx config
operator ALL=(root) NOPASSWD: \
    /bin/systemctl start mywebapp, \
    /bin/systemctl stop mywebapp, \
    /bin/systemctl restart mywebapp, \
    /bin/systemctl status mywebapp, \
    /bin/systemctl reload nginx
EOF
chmod 440 /etc/sudoers.d/operator

# =============================================================================
# 3. MariaDB — create database and user
# =============================================================================
info "Configuring MariaDB…"
systemctl enable --now mariadb

# Generate a random DB password if env file doesn't already exist
if [[ ! -f "$ENV_FILE" ]]; then
    DB_PASSWORD=$(openssl rand -base64 24 | tr -dc 'A-Za-z0-9' | head -c 32)
else
    DB_PASSWORD=$(grep MYWEBAPP_DB_PASSWORD "$ENV_FILE" | cut -d= -f2)
fi

mysql -u root <<SQL
CREATE DATABASE IF NOT EXISTS \`${DB_NAME}\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '${DB_USER}'@'127.0.0.1' IDENTIFIED BY '${DB_PASSWORD}';
GRANT ALL PRIVILEGES ON \`${DB_NAME}\`.* TO '${DB_USER}'@'127.0.0.1';
FLUSH PRIVILEGES;
SQL

# =============================================================================
# 4. Build and install the web application
# =============================================================================
info "Cloning repository…"
rm -rf "$SRC_DIR"
git clone --depth 1 "$REPO_URL" "$SRC_DIR"

info "Publishing application (Release)…"
dotnet publish "$SRC_DIR/Lab1/src/MyWebApp/Lab1.csproj" \
    -c Release \
    --runtime linux-x64 \
    --self-contained false \
    -o "$APP_DIR"

chown -R app:app "$APP_DIR"
chmod 750 "$APP_DIR"

# =============================================================================
# 5. Configuration (env file)
# =============================================================================
info "Writing environment file…"
mkdir -p /etc/mywebapp
cat > "$ENV_FILE" <<EOF
MYWEBAPP_DB_HOST=127.0.0.1
MYWEBAPP_DB_PORT=3306
MYWEBAPP_DB_NAME=${DB_NAME}
MYWEBAPP_DB_USER=${DB_USER}
MYWEBAPP_DB_PASSWORD=${DB_PASSWORD}
EOF
chmod 640 "$ENV_FILE"
chown root:app "$ENV_FILE"

# =============================================================================
# 6. systemd service + socket
# =============================================================================
info "Installing systemd units…"
cp "$SRC_DIR/Lab1/deploy/mywebapp.socket"  /etc/systemd/system/mywebapp.socket
cp "$SRC_DIR/Lab1/deploy/mywebapp.service" /etc/systemd/system/mywebapp.service

systemctl daemon-reload
systemctl enable mywebapp.socket
systemctl enable mywebapp.service
systemctl start  mywebapp.socket

info "Starting mywebapp…"
systemctl start mywebapp

# =============================================================================
# 7. Nginx
# =============================================================================
info "Configuring Nginx…"
cp "$SRC_DIR/Lab1/deploy/nginx.conf" /etc/nginx/sites-available/mywebapp
ln -sf /etc/nginx/sites-available/mywebapp /etc/nginx/sites-enabled/mywebapp
rm -f /etc/nginx/sites-enabled/default   # disable default site

nginx -t
systemctl enable --now nginx
systemctl reload nginx

# =============================================================================
# 8. Lock default distro user (ubuntu / vagrant / cloud)
# =============================================================================
info "Locking default system users…"
for default_user in ubuntu vagrant cloud; do
    if id "$default_user" &>/dev/null; then
        passwd --lock "$default_user"
        usermod --expiredate 1 "$default_user"
        info "  Locked user: $default_user"
    fi
done

# =============================================================================
# Done
# =============================================================================
info "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
info "Installation complete!"
info ""
info "  DB password stored in: $ENV_FILE"
info ""
info "  Test health:  curl http://localhost/health/alive  (via direct app port)"
info "  Test list:    curl -H 'Accept: application/json' http://localhost/items"
info ""
info "  Users: student / teacher / operator"
info "  Default password: $DEFAULT_PASSWORD (must be changed on first login)"
info "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
