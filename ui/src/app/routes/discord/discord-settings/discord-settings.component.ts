import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { PopupComponent, PopupInstance, PopupService } from 'src/app/components';
import { AuthService, SubscriptionHandler } from 'src/app/services';
import { DiscordGuild, DiscordService, DiscordSettings } from '../discord.service';

class SettingsMap {

    get authUsers() {
        return this.settings.authedUsers.join(', ');
    }
    set authUsers(value: string) {
        this.settings.authedUsers = value.split(',').map(t => t.trim());
    }

    get mangaIds() {
        return this.settings.mangaUpdatesIds.join(', ');
    }
    set mangaIds(value: string) {
        this.settings.mangaUpdatesIds = value.split(',').map(t => t.trim());
    }

    constructor(
        public guild: DiscordGuild,
        public settings: DiscordSettings,
        public image: string
    ) { }
}

@Component({
    selector: 'cba-discord-settings',
    templateUrl: './discord-settings.component.html',
    styleUrls: ['./discord-settings.component.scss']
})
export class DiscordSettingsComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();
    private _ins?: PopupInstance;

    loading: boolean = false;

    guilds: DiscordGuild[] = [];
    settings: DiscordSettings[] = [];
    data: SettingsMap[] = [];

    selected?: SettingsMap;
    original?: SettingsMap;

    @ViewChild('popup') popup!: PopupComponent;

    get isLoggedIn() { return !!this.auth.currentUser; }

    constructor(
        private api: DiscordService,
        private auth: AuthService,
        private pop: PopupService
    ) { }

    ngOnInit() {
        this._subs
            .subscribe(this.auth.onLogin, t => {
                this.process();
            });
    }

    ngOnDestroy() {
        this._subs.unsubscribe();
    }

    private async process() {
        if (!this.isLoggedIn || this.loading) return;

        this.loading = true;
        await this.getData();
        this.loading = false;
    }

    private async getData(force?: boolean) {
        if (this.data.length > 0 && !force) return;

        const [
            guilds,
            settings
        ] = await Promise.all([
            this.api.guilds().promise,
            this.api.settings().promise
        ]);

        this.guilds = guilds;
        this.settings = settings;

        this.data = this.guilds.map(t => {
            let settings = this.settings.find(a => a.guildId.toString() === t.id);

            if (!settings) {
                settings = {
                    id: -1,
                    createdAt: new Date(),
                    updatedAt: new Date(),
                    guildId: t.id,
                    authedUsers: [t.ownerId],
                    enableLookup: false,
                    enableTheft: false,
                    mangaUpdatesIds: [],
                    mangaUpdatesNsfw: false,
                    enableTwitterUrls: true,
                };
            }

            return new SettingsMap(t, settings, this.api.avatar(t));
        });
    }

    async save() {
        if (!this.selected) return;

        this._ins?.cancel();
        this.loading = true;
        let updated = await this.api.settings(this.selected.settings).promise;

        const ci = this.data.findIndex(t => t.guild.id === updated.guildId);
        if (ci === -1) {
            this.loading = false;
            return;
        }

        this.data[ci] = this.selected;
        this.loading = false;
    }

    async cancel() {
        this.loading = true;
        this._ins?.cancel();
        this.loading = false;
    }

    open(item: SettingsMap) {
        this.selected = new SettingsMap(
            this.clone(item.guild),
            this.clone(item.settings),
            item.image
        );
        this._ins = this.pop.show(this.popup);
    }

    clone<T>(item: T): T {
        return JSON.parse(JSON.stringify(item));
    }
}
