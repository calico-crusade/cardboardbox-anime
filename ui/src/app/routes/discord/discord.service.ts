import { Injectable } from "@angular/core";
import { HttpService, RxjsHttpResp } from "src/app/services";

export interface DiscordSettings {
    id: number;

    createdAt?: Date;
    updatedAt?: Date;
    deletedAt?: Date;

    guildId: string;
    authedUsers: string[];
    enableLookup: boolean;
    enableTheft: boolean;
    
    mangaUpdatesChannel?: string;
    mangaUpdatesIds: string[];
    mangaUpdatesNsfw: boolean;
}

export interface DiscordUser {
    isBot: boolean;
    username: string;
    discriminatorValue: number;
    avatarId?: string;
    bannerId?: string;
    accentColor?: {
        rawvalue: number;
        r: number;
        g: number;
        b: number;
    };
    publicFlags: number;
    createdAt: Date;
    discriminator: string;
    mention: string;
    status: string;
    id: string;
}

export interface DiscordGuildUser extends DiscordUser {
    nickname: string;
    roleIds: string[];
}

export interface DiscordChannel {
    topic?: string;
    slowModeInterval: number;
    categoryId: string;
    mention: string;
    isNsfw: boolean;
    permissionOverwrites: {
        targetId: number;
        targetType: number;
        permissions: {
            "allowValue": number,
            "denyValue": number,
            "createInstantInvite": number,
            "manageChannel": number,
            "addReactions": number,
            "viewChannel": number,
            "sendMessages": number,
            "sendTTSMessages": number,
            "manageMessages": number,
            "embedLinks": number,
            "attachFiles": number,
            "readMessageHistory": number,
            "mentionEveryone": number,
            "useExternalEmojis": number,
            "connect": number,
            "speak": number,
            "muteMembers": number,
            "deafenMembers": number,
            "moveMembers": number,
            "useVAD": number,
            "prioritySpeaker": number,
            "stream": number,
            "manageRoles": number,
            "manageWebhooks": number,
            "useApplicationCommands": number,
            "requestToSpeak": number,
            "manageThreads": number,
            "createPublicThreads": number,
            "createPrivateThreads": number,
            "useExternalStickers": number,
            "sendMessagesInThreads": number,
            "startEmbeddedActivities": number
        }
    }[];
    name: string;
    position: number;
    guildId: string;
    createdAt: Date;
    id: string;
}

export interface DiscordRole {
    color?: {
        rawvalue: number;
        r: number;
        g: number;
        b: number;
    };
    isHoisted: boolean;
    isManaged: boolean;
    isMentionable: boolean;
    name: string;
    icon?: string;
    emoji: {
        name?: string;
    };
    permissions: {
        "rawValue": number,
        "createInstantInvite": boolean,
        "banMembers": boolean,
        "kickMembers": boolean,
        "administrator": boolean,
        "manageChannels": boolean,
        "manageGuild": boolean,
        "addReactions": boolean,
        "viewAuditLog": boolean,
        "viewGuildInsights": boolean,
        "viewChannel": boolean,
        "sendMessages": boolean,
        "sendTTSMessages": boolean,
        "manageMessages": boolean,
        "embedLinks": boolean,
        "attachFiles": boolean,
        "readMessageHistory": boolean,
        "mentionEveryone": boolean,
        "useExternalEmojis": boolean,
        "connect": boolean,
        "speak": boolean,
        "muteMembers": boolean,
        "deafenMembers": boolean,
        "moveMembers": boolean,
        "useVAD": boolean,
        "prioritySpeaker": boolean,
        "stream": boolean,
        "changeNickname": boolean,
        "manageNicknames": boolean,
        "manageRoles": boolean,
        "manageWebhooks": boolean,
        "manageEmojisAndStickers": boolean,
        "useApplicationCommands": boolean,
        "requestToSpeak": boolean,
        "manageEvents": boolean,
        "manageThreads": boolean,
        "createPublicThreads": boolean,
        "createPrivateThreads": boolean,
        "useExternalStickers": boolean,
        "sendMessagesInThreads": boolean,
        "startEmbeddedActivities": boolean,
        "moderateMembers": boolean
    },
    position: number;
    tags?: any;
    createdAt: Date;
    isEveryone: boolean;
    mention: string;
    id: string;
}

export interface DiscordGuild  {
    roles: DiscordRole[];
    emotes: {
        isManaged: boolean;
        requireColons: boolean;
        roleIds: any[];
        creatorId?: any;
        name: string;
        id: any;
        animated: boolean;
        createdAt: Date;
        url: string;
    }[];
    name: string;
    id: string;
    afkTimeout: number;
    isWidgetEnabled: boolean;
    verificationLevel: number;
    mfaLevel: number;
    defaultMessageNotifications: number;
    explicitContentFilter: number;
    afkChannelId: number;
    widgetChannelId: string;
    systemChannelId: string;
    rulesChannelId: string;
    publicUpdatesChannelId: string;
    ownerId: string;
    voiceRegionId: string;
    iconId: string;
    splashId: string;
    discoverySplashId: string;
    applicationId: number;
    premiumTier: number;
    bannerId: string;
    vanityURLCode: string;
    systemChannelFlags: number;
    description?: any;
    premiumSubscriptionCount: number;
    preferredLocale: string;
    maxPresences: number;
    maxMembers: number;
    maxVideoChannelUsers: number;
    approximateMemberCount: number;
    approximatePresenceCount: number;
    nsfwLevel: number;
    features: {
        value: number;
        experimental: any[];
        hasThreads: boolean;
        hasTextInVoice: boolean;
        isStaffServer: boolean;
        isHub: boolean;
        isLinkedToHub: boolean;
        isPartnered: boolean;
        isVerified: boolean;
        hasVanityUrl: boolean;
        hasRoleSubscriptions: boolean;
        hasRoleIcons: boolean;
        hasPrivateThreads: boolean;
    };
}

@Injectable({ providedIn: 'root' })
export class DiscordService {
    constructor(
        private http: HttpService
    ) { }

    settings(): RxjsHttpResp<DiscordSettings[]>;
    settings(id: number): RxjsHttpResp<DiscordSettings>;
    settings(settings: DiscordSettings): RxjsHttpResp<DiscordSettings>;
    settings(item?: number | DiscordSettings) {

        if (!item) return this.http.get<DiscordSettings[]>('discord/settings');
        if (typeof item === 'number') return this.http.get<DiscordSettings>(`discord/settings/${item}`);

        return this.http.post<DiscordSettings>('discord/settings', item);
    }

    guilds(): RxjsHttpResp<DiscordGuild[]>;
    guilds(id: string | number): RxjsHttpResp<DiscordGuild>;
    guilds(id?: string | number) {
        if (id) return this.http.get<DiscordGuild>(`discord/guild/${id}`);
        return this.http.get<DiscordGuild[]>(`discord/guilds`);
    }

    user(id: number | string): RxjsHttpResp<DiscordUser>;
    user(id: number | string, guildId: number | string): RxjsHttpResp<DiscordGuildUser>;
    user(id: number | string, guildId?: number | string) {
        if (!guildId) return this.http.get<DiscordUser>(`discord/user/${id}`);
        return this.http.get<DiscordGuildUser>(`discord/guild/${guildId}/user/${id}`);
    }

    avatar(user: DiscordUser): string;
    avatar(user: DiscordGuildUser): string;
    avatar(guild: DiscordGuild): string;
    avatar(user: DiscordUser | DiscordGuildUser | DiscordGuild) {

        if ('ownerId' in user) {
            const { iconId, id } = user;
            if (!iconId) return '/assets/error.gif';

            const animated = iconId?.startsWith('a_');
            return `https://cdn.discordapp.com/icons/${id}/${iconId || ''}.${(animated ? 'gif' : 'png')}`;
        }

        const { avatarId, id } = user;
        if (!avatarId) return '/assets/error.gif';
        const animated = avatarId?.startsWith('a_') || false;
        return `https://cdn.discordapp.com/avatars/${id}/${avatarId || ''}.${(animated ? 'gif' : 'png')}`;
    }
}