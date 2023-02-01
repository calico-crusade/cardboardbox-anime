import { environment } from "src/environments/environment";
import { checkPlatform, StorageVar } from "./storage-var";

const STORE_TOKEN = 'AuthToken';

export abstract class ConfigObject {

    private _token = new StorageVar<string | null>(null, STORE_TOKEN);

    get defaultTitle() { return 'CardboardBox | Anime'; }
    get apiUrl() { return environment.apiUrl; }
    get appId() { return environment.appId; }
    get authUrl() { return environment.authUrl; }
    get isProd() { return environment.production; }

    get token() { return this._token.value; }
    set token(value: string | null) {
        this._token.value = value;
    }

    get platform() { return checkPlatform(); }
}