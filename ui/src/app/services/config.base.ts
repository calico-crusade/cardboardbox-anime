import { environment } from "src/environments/environment";

const STORE_TOKEN = 'AuthToken';

export abstract class ConfigObject {

    get apiUrl() { return environment.apiUrl; }
    get appId() { return environment.appId; }
    get isProd() { return environment.production; }

    get token() { return localStorage.getItem(STORE_TOKEN); }

    set token(value: string | null) {
        if (!value) {
            localStorage.removeItem(STORE_TOKEN);
            return;
        }

        localStorage.setItem(STORE_TOKEN, value); 
    }
}