import { environment } from "src/environments/environment";
import { Capacitor } from "@capacitor/core";

const STORE_TOKEN = 'AuthToken';
const DEFAULT_PLATFORM = 'web';

export abstract class ConfigObject {

    get defaultTitle() { return 'CardboardBox | Anime'; }
    get apiUrl() { return environment.apiUrl; }
    get appId() { return environment.appId; }
    get authUrl() { return environment.authUrl; }
    get isProd() { return environment.production; }

    get token() { return localStorage.getItem(STORE_TOKEN); }

    set token(value: string | null) {
        if (!value) {
            localStorage.removeItem(STORE_TOKEN);
            return;
        }

        localStorage.setItem(STORE_TOKEN, value); 
    }

    get platform() {
        try {
            return Capacitor?.getPlatform() || DEFAULT_PLATFORM;
        } catch(err) {
            console.error('Failure to fetch platform', { err });
            return DEFAULT_PLATFORM;
        }
    }
}