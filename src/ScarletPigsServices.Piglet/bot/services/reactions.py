import discord
import emoji as emoji_lib


async def get_reactions_from_message(message: discord.Message):
    msg_reactions = message.reactions
    if not msg_reactions:
        return None

    reactions = []
    for reaction in msg_reactions:
        name = reaction.emoji if not reaction.is_custom_emoji() else reaction.emoji.name  # type: ignore
        reactions.append({
            'emoji_name': name,
            'reactors': set([user async for user in reaction.users()])
        })

    all_reactors = set()
    for reaction in reactions:
        all_reactors.update(reaction['reactors'])
    all_reactors = list(all_reactors)

    user_reactions = {reactor: set() for reactor in all_reactors}
    for reaction in reactions:
        for reactor in reaction['reactors']:
            user_reactions[reactor].add(reaction['emoji_name'])

    header_row = ["Name"]
    for reaction in reactions:
        header_row.append(reaction["emoji_name"])

    player_rows = []
    for reactor in all_reactors:
        player_row = [reactor.display_name]
        for reaction in reactions:
            player_row.append(
                "X" if reaction['emoji_name'] in user_reactions[reactor] else "")
        player_rows.append(player_row)

    return [header_row] + player_rows


def get_emojis_in_message(message: str):
    emoji_list = []
    for emoji in message.split():
        if emoji.startswith("<:"):
            tmp = emoji.replace("><", " ").replace(
                ">", " ").replace("<", " ").replace(":", " ").split()
            for e in tmp:
                if str.isnumeric(e):
                    emoji_list.append(int(e))
        else:
            for e in emoji_lib.distinct_emoji_list(emoji):
                emoji_list.append(emoji_lib.demojize(e))
    return emoji_list


async def remove_reaction_if_not_member(message, reaction, user):
    guild = message.guild
    try:
        await guild.fetch_member(user.id)
    except discord.NotFound:
        await message.remove_reaction(reaction.emoji, user)
        return user
    return None


async def process_reaction(message, reaction: discord.Reaction):
    del message
    bot_reaction = 1 if reaction.me else 0
    return max(reaction.count - bot_reaction, 0)