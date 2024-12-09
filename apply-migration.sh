#!/bin/bash
function _dbg_print() {
    if [[ "${DEBUG}" -eq 1 ]]; then
        echo "[DEBUG] $@"
    fi
}


if [[ -f ".env" ]]; then
    source .env
    _dbg_print "Loaded environment file '.env'"
else
    _dbg_print "No environment file found"
fi


function _build_connection_string() {
    if [[ -z "${DATABASE_CONNECTION_STRING}" ]]; then
        _dbg_print "DATABASE_CONNECTION_STRING not found, building from individual environment variables"
        _hn="${DATABASE_HOST}"
        if [[ -z "${_hn}" ]]; then
            _hn="db"
        fi

        if [[ -n "${DATABASE_PORT}" ]]; then
            _hn="${_hn}:${DATABASE_PORT}"
        fi

        _working=""
        if [[ -n "${_hn}" ]]; then
            _working="Host=${_hn}"
        fi

        if [[ -n "${DATABASE_NAME}" ]]; then
            _working="${_working};Database=${DATABASE_NAME}"
        else
            _working="${_working};Database=kasta"
        fi

        if [[ -n "${DATABASE_USER}" ]]; then
            _working="${_working};Username=${DATABASE_USER}"
        else
            _working="${_working};Username=postgres"
        fi
        
        if [[ -n "${DATABASE_PASSWORD}" ]]; then
            _working="${_working};Password=${DATABASE_PASSWORD}"
        fi

        if [[ -n "${DATABASE_APPEND}" ]]; then
            _working="${_working};${DATABASE_APPEND}"
        fi

        echo $_working
    else
        echo $DATABASE_CONNECTION_STRING
    fi
    return 0
}

_connection_string=$(_build_connection_string)

if [[ -z "${_connection_string}" ]]; then
    echo "Missing required environment variable (see README.md, section 'Migration Script')"
    exit 1
fi
if [[ -z "${1}" ]]; then
    echo "Missing migration name"
    echo "Usage: ./apply-migration.sh [name]"
    exit 1
fi

_dbg_print "Connection String:   $_connection_string"
_dbg_print "   Migration Name:   $1"

dotnet ef database update $1 --project "Kasta.Data" --context "Kasta.Data.ApplicationDbContext" --connection "$DATABASE_CONNECTION_STRING"